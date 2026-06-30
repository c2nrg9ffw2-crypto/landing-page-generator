using MailKit;
using MailKit.Net.Imap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PCH.Core.Interfaces;
using PCH.Core.Models;
using PCH.Data;

namespace PCH.Connectors;

/// <summary>
/// Fetches the last 20 emails from an IMAP inbox, applies a keyword pre-filter,
/// and for matching emails calls the local LLM to classify and summarise them.
/// Non-Other classified emails auto-create a <see cref="TaskItem"/> (deduplicated by Message-ID).
/// </summary>
public class EmailConnector : IConnector
{
    private static readonly string[] Keywords =
        ["booking", "reservation", "deadline", "meeting", "invoice"];

    private readonly PchDbContext _db;
    private readonly LlmClassifier _llm;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailConnector> _logger;

    /// <inheritdoc />
    public string Name => "Email";

    /// <summary>Initialises the connector with its dependencies.</summary>
    public EmailConnector(
        PchDbContext db,
        LlmClassifier llm,
        IConfiguration config,
        ILogger<EmailConnector> logger)
    {
        _db = db;
        _llm = llm;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Connects via IMAP, fetches the last 20 messages, keyword-filters them,
    /// classifies matches via LLM, auto-creates Tasks, and stores everything in the
    /// <c>emails</c> table.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of new emails stored.</returns>
    public async Task<int> FetchAsync(CancellationToken cancellationToken = default)
    {
        var host     = _config["Email:ImapHost"]  ?? throw new InvalidOperationException("Email:ImapHost not configured");
        var port     = int.Parse(_config["Email:ImapPort"] ?? "993");
        var useSsl   = bool.Parse(_config["Email:ImapSsl"]  ?? "true");
        var username = _config["Email:Username"]  ?? throw new InvalidOperationException("Email:Username not configured (set via user-secrets)");
        var password = _config["Email:Password"]  ?? throw new InvalidOperationException("Email:Password not configured (set via user-secrets)");

        _logger.LogInformation("Email sync started ({Host}:{Port})", host, port);

        using var imap = new ImapClient();
        await imap.ConnectAsync(host, port, useSsl, cancellationToken);
        await imap.AuthenticateAsync(username, password, cancellationToken);

        var inbox = imap.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var total = inbox.Count;
        if (total == 0)
        {
            await imap.DisconnectAsync(true, cancellationToken);
            _logger.LogInformation("Inbox is empty — nothing to sync");
            return 0;
        }

        var startIdx = Math.Max(0, total - 20);
        var summaries = await inbox.FetchAsync(startIdx, total - 1, MessageSummaryItems.UniqueId, cancellationToken);

        var newCount = 0;

        foreach (var summary in summaries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message   = await inbox.GetMessageAsync(summary.Index, cancellationToken);
            var messageId = !string.IsNullOrWhiteSpace(message.MessageId)
                ? message.MessageId
                : $"idx-{summary.Index}-{inbox.UidValidity}";

            if (await _db.Emails.AnyAsync(e => e.MessageId == messageId, cancellationToken))
                continue;

            var subject     = message.Subject ?? "(no subject)";
            var sender      = message.From.FirstOrDefault()?.ToString() ?? "Unknown";
            var bodyPreview = ExtractBodyPreview(message.TextBody ?? message.HtmlBody ?? "");
            var receivedAt  = message.Date == default ? DateTimeOffset.UtcNow : message.Date;

            var isMatch   = ContainsKeyword(subject, bodyPreview);
            var emailType = EmailType.Other;
            string? llmSummary = null;
            int? linkedTaskId  = null;

            if (isMatch)
            {
                (emailType, llmSummary) = await _llm.ClassifyAsync(subject, sender, bodyPreview, cancellationToken);

                if (emailType != EmailType.Other)
                    linkedTaskId = await CreateTaskAsync(subject, sender, bodyPreview, emailType, messageId, cancellationToken);
            }

            _db.Emails.Add(new Email
            {
                MessageId      = messageId,
                Subject        = Truncate(subject, 1000),
                Sender         = Truncate(sender, 512),
                ReceivedAt     = receivedAt,
                BodyPreview    = bodyPreview,
                IsKeywordMatch = isMatch,
                EmailType      = emailType,
                LlmSummary     = llmSummary,
                LinkedTaskId   = linkedTaskId,
                FetchedAt      = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            newCount++;
        }

        await imap.DisconnectAsync(true, cancellationToken);
        _logger.LogInformation("Email sync complete: {NewCount} new email(s) ingested", newCount);
        return newCount;
    }

    private async Task<int?> CreateTaskAsync(
        string subject, string sender, string bodyPreview,
        EmailType emailType, string messageId, CancellationToken ct)
    {
        var existing = await _db.Tasks.FirstOrDefaultAsync(t => t.ExternalId == messageId, ct);
        if (existing is not null)
            return existing.Id;

        var category = emailType switch
        {
            EmailType.Booking  => TaskCategory.Booking,
            EmailType.Deadline => TaskCategory.Work,
            EmailType.Meeting  => TaskCategory.Work,
            EmailType.Invoice  => TaskCategory.Finance,
            _                  => TaskCategory.General
        };

        var now  = DateTimeOffset.UtcNow;
        var task = new TaskItem
        {
            Title       = Truncate($"[{emailType}] {subject}", 256),
            Description = $"From: {sender}\n\n{Truncate(bodyPreview, 400)}",
            Category    = category,
            Status      = TaskState.Todo,
            Source      = ItemSource.Email,
            ExternalId  = messageId,
            CreatedAt   = now,
            UpdatedAt   = now
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Auto-created task #{TaskId} from email '{Subject}'", task.Id, subject);
        return task.Id;
    }

    private static bool ContainsKeyword(string subject, string body)
    {
        var text = (subject + " " + body).ToLowerInvariant();
        return Keywords.Any(kw => text.Contains(kw));
    }

    private static string ExtractBodyPreview(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        // Collapse whitespace runs so the preview is dense and readable
        var flat = string.Join(" ", raw.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
        return Truncate(flat.Trim(), 500);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max];
}
