namespace PCH.Core.Models;

/// <summary>
/// A raw email message fetched from the IMAP inbox.
/// Emails that pass the keyword pre-filter are classified by the local LLM.
/// Classified emails with a non-Other type auto-generate a <see cref="TaskItem"/>.
/// Persisted to the <c>emails</c> table.
/// </summary>
public class Email
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>IMAP Message-ID header; used for deduplication across syncs.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Email subject line.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Sender display name and/or address.</summary>
    public string Sender { get; set; } = string.Empty;

    /// <summary>When the email was sent/received.</summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>First ~500 characters of the plain-text body.</summary>
    public string BodyPreview { get; set; } = string.Empty;

    /// <summary>True when at least one keyword matched in the pre-filter.</summary>
    public bool IsKeywordMatch { get; set; }

    /// <summary>LLM-assigned category. Only meaningful when <see cref="IsKeywordMatch"/> is true.</summary>
    public EmailType EmailType { get; set; } = EmailType.Other;

    /// <summary>1–2 sentence LLM summary, or null if the email was not classified.</summary>
    public string? LlmSummary { get; set; }

    /// <summary>Id of the auto-created task, or null if no task was generated.</summary>
    public int? LinkedTaskId { get; set; }

    /// <summary>UTC timestamp when this email was fetched and stored.</summary>
    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
}
