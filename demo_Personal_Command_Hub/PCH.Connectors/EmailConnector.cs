using System.Globalization;
using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using PCH.Core.Interfaces;
using PCH.Core.Models;
using PCH.Data;

namespace PCH.Connectors;

/// <summary>
/// Fetches the last 20 emails from an IMAP inbox, applies a keyword pre-filter,
/// and for matching emails calls the local LLM to classify and summarise them.
/// Non-Other classified emails auto-create a <see cref="TaskItem"/> (deduplicated by Message-ID).
/// Emails from the Zoezi gym-booking platform are detected and parsed deterministically
/// (see <see cref="TryParseZoeziBooking"/>) instead of going through the LLM.
/// </summary>
public class EmailConnector : IConnector
{
    private static readonly string[] Keywords =
        ["booking", "reservation", "deadline", "meeting", "invoice"];

    private const string ZoeziSenderDomain = "gymsystem.se";
    private const string ZoeziPlatformName = "Zoezi";

    // Subject example: "Pilates Flow startar 2026-07-05 18:00. Välkommen!"
    private static readonly Regex ZoeziSubjectRegex = new(
        @"^(?<class>.+?)\s+startar\s+(?<datetime>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Body example: "Din bokning/lektion:\nPilates Flow\nStartar: 2026-07-05 18:00"
    private static readonly Regex ZoeziBodyRegex = new(
        @"Din bokning/lektion:\s*(?<class>.+?)\s*(?:\r?\n)+\s*Startar:\s*(?<datetime>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            var subject       = message.Subject ?? "(no subject)";
            var sender        = message.From.FirstOrDefault()?.ToString() ?? "Unknown";
            var senderAddress = (message.From.FirstOrDefault() as MailboxAddress)?.Address ?? "";
            var rawBody       = message.TextBody ?? message.HtmlBody ?? "";
            var bodyPreview   = ExtractBodyPreview(rawBody);
            var receivedAt    = message.Date == default ? DateTimeOffset.UtcNow : message.Date;

            var isZoeziBooking = IsZoeziBooking(senderAddress, subject);
            var isMatch        = isZoeziBooking || ContainsKeyword(subject, bodyPreview);
            var emailType      = EmailType.Other;
            string? llmSummary = null;
            int? linkedTaskId  = null;

            if (isZoeziBooking)
            {
                emailType = EmailType.Booking;
                linkedTaskId = await CreateZoeziBookingAsync(subject, senderAddress, rawBody, messageId, cancellationToken);

                if (linkedTaskId is null)
                    _logger.LogWarning("Email from {Sender} matched Zoezi detection but the class name/start time could not be parsed: '{Subject}'", senderAddress, subject);
            }
            else if (isMatch)
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

    /// <summary>
    /// True if this email looks like a Zoezi (gymsystem.se) class-booking confirmation:
    /// either the sender domain is gymsystem.se, or the subject has the "X startar &lt;date&gt;. Välkommen!" shape.
    /// </summary>
    private static bool IsZoeziBooking(string senderAddress, string subject)
    {
        var domainMatch = senderAddress.EndsWith("@" + ZoeziSenderDomain, StringComparison.OrdinalIgnoreCase);
        var subjectMatch = subject.Contains("startar", StringComparison.OrdinalIgnoreCase)
                         && subject.Contains("Välkommen", StringComparison.OrdinalIgnoreCase);
        return domainMatch || subjectMatch;
    }

    /// <summary>
    /// Parses the class name and start time from a Zoezi booking email (deterministic, no LLM),
    /// then creates a <see cref="Booking"/> and a matching "Attend &lt;class&gt;" <see cref="TaskItem"/>,
    /// both deduplicated by the email's Message-ID.
    /// </summary>
    /// <returns>The id of the linked task, or null if the email couldn't be parsed.</returns>
    private async Task<int?> CreateZoeziBookingAsync(
        string subject, string senderAddress, string rawBody, string messageId, CancellationToken ct)
    {
        if (!TryParseZoeziBooking(subject, rawBody, out var className, out var startTime))
            return null;

        if (!await _db.Bookings.AnyAsync(b => b.ExternalId == messageId, ct))
        {
            _db.Bookings.Add(new Booking
            {
                Title      = className,
                StartTime  = startTime,
                EndTime    = startTime.AddHours(1),
                Source     = ItemSource.Booking,
                Platform   = ZoeziPlatformName,
                ExternalId = messageId,
                CreatedAt  = DateTimeOffset.UtcNow
            });
        }

        var existingTask = await _db.Tasks.FirstOrDefaultAsync(t => t.ExternalId == messageId, ct);
        if (existingTask is not null)
        {
            await _db.SaveChangesAsync(ct);
            return existingTask.Id;
        }

        var now = DateTimeOffset.UtcNow;
        var task = new TaskItem
        {
            Title       = Truncate($"Attend {className}", 256),
            Description = $"Zoezi booking confirmation from {senderAddress}",
            Category    = TaskCategory.Personal,
            DueDate     = startTime,
            Status      = TaskState.Todo,
            Source      = ItemSource.Booking,
            ExternalId  = messageId,
            CreatedAt   = now,
            UpdatedAt   = now
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Auto-created Zoezi booking + task #{TaskId} for '{ClassName}' at {StartTime}", task.Id, className, startTime);
        return task.Id;
    }

    /// <summary>
    /// Extracts the class name and start time from the subject line
    /// ("&lt;class&gt; startar yyyy-MM-dd HH:mm. Välkommen!"), falling back to parsing
    /// "Din bokning/lektion: &lt;class&gt; ... Startar: yyyy-MM-dd HH:mm" from the body
    /// when the subject doesn't match cleanly (e.g. non-standard wording or truncation).
    /// </summary>
    private static bool TryParseZoeziBooking(string subject, string rawBody, out string className, out DateTimeOffset startTime)
    {
        var subjectMatch = ZoeziSubjectRegex.Match(subject);
        if (subjectMatch.Success && TryParseSwedishDateTime(subjectMatch.Groups["datetime"].Value, out startTime))
        {
            className = subjectMatch.Groups["class"].Value.Trim();
            return true;
        }

        var bodyMatch = ZoeziBodyRegex.Match(rawBody);
        if (bodyMatch.Success && TryParseSwedishDateTime(bodyMatch.Groups["datetime"].Value, out startTime))
        {
            className = bodyMatch.Groups["class"].Value.Trim();
            return true;
        }

        className = string.Empty;
        startTime = default;
        return false;
    }

    /// <summary>Parses a "yyyy-MM-dd HH:mm" Zoezi timestamp as local time (the gym's timezone).</summary>
    private static bool TryParseSwedishDateTime(string raw, out DateTimeOffset result)
    {
        if (DateTime.TryParseExact(
                raw.Trim(), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            result = new DateTimeOffset(parsed, TimeZoneInfo.Local.GetUtcOffset(parsed));
            return true;
        }

        result = default;
        return false;
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
