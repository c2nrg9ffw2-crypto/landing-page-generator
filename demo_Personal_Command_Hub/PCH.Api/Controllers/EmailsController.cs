using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCH.Connectors;
using PCH.Core.Dtos;
using PCH.Data;

namespace PCH.Api.Controllers;

/// <summary>
/// Endpoints for reading stored emails and triggering an IMAP sync.
/// </summary>
[ApiController]
[Route("api/emails")]
public class EmailsController : ControllerBase
{
    private readonly PchDbContext _db;
    private readonly EmailConnector _connector;

    /// <summary>Creates the controller.</summary>
    /// <param name="db">The PCH database context.</param>
    /// <param name="connector">The email connector used to trigger a sync.</param>
    public EmailsController(PchDbContext db, EmailConnector connector)
    {
        _db = db;
        _connector = connector;
    }

    /// <summary>Returns all stored emails, newest first.</summary>
    /// <param name="ct">Request cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmailResponseDto>>> GetAll(CancellationToken ct)
    {
        // SQLite cannot ORDER BY DateTimeOffset — sort in memory after materialising.
        var emails = await _db.Emails.ToListAsync(ct);
        return Ok(emails
            .OrderByDescending(e => e.ReceivedAt)
            .Select(e => new EmailResponseDto(
                e.Id, e.MessageId, e.Subject, e.Sender, e.ReceivedAt,
                e.BodyPreview, e.IsKeywordMatch, e.EmailType, e.LlmSummary,
                e.LinkedTaskId, e.FetchedAt)));
    }

    /// <summary>Triggers an IMAP sync and returns the count of new emails stored.</summary>
    /// <param name="ct">Request cancellation token.</param>
    [HttpPost("sync")]
    public async Task<ActionResult<object>> Sync(CancellationToken ct)
    {
        try
        {
            var count = await _connector.FetchAsync(ct);
            return Ok(new { newEmails = count });
        }
        catch (AuthenticationException ex)
        {
            return StatusCode(401, new { error = "IMAP authentication failed. Use a Microsoft app password, not your account password.", detail = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not configured"))
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
