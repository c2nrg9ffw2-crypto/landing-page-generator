using PCH.Core.Models;

namespace PCH.Core.Dtos;

/// <summary>
/// Represents a stored email as returned by <c>GET /api/emails</c>.
/// </summary>
/// <param name="Id">Database primary key.</param>
/// <param name="MessageId">IMAP Message-ID used for deduplication.</param>
/// <param name="Subject">Email subject line.</param>
/// <param name="Sender">Sender display name / address.</param>
/// <param name="ReceivedAt">When the email was sent/received.</param>
/// <param name="BodyPreview">First ~500 chars of the plain-text body.</param>
/// <param name="IsKeywordMatch">True when at least one keyword matched the pre-filter.</param>
/// <param name="EmailType">LLM-assigned category (meaningful only when <paramref name="IsKeywordMatch"/> is true).</param>
/// <param name="LlmSummary">1–2 sentence LLM summary, or null.</param>
/// <param name="LinkedTaskId">Id of the auto-created task, or null.</param>
/// <param name="FetchedAt">When this email was fetched and stored.</param>
public record EmailResponseDto(
    int Id,
    string MessageId,
    string Subject,
    string Sender,
    DateTimeOffset ReceivedAt,
    string BodyPreview,
    bool IsKeywordMatch,
    EmailType EmailType,
    string? LlmSummary,
    int? LinkedTaskId,
    DateTimeOffset FetchedAt);
