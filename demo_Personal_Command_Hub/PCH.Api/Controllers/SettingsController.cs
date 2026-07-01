using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCH.Core.Dtos;
using PCH.Core.Models;
using PCH.Data;

namespace PCH.Api.Controllers;

/// <summary>
/// Endpoints for reading and updating app settings:
/// email/IMAP config, notification preferences, and RSS feed list.
/// </summary>
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly PchDbContext _db;

    /// <summary>Creates the controller.</summary>
    public SettingsController(PchDbContext db) => _db = db;

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<AppSettings> GetOrCreateSettingsAsync(CancellationToken ct)
    {
        var s = await _db.AppSettings.FirstOrDefaultAsync(ct);
        if (s is not null)
            return s;
        s = new AppSettings { Id = 1 };
        _db.AppSettings.Add(s);
        await _db.SaveChangesAsync(ct);
        return s;
    }

    // ── Email / IMAP ─────────────────────────────────────────────────────────

    /// <summary>Returns the stored IMAP email configuration.</summary>
    [HttpGet("email")]
    public async Task<ActionResult<EmailSettingsDto>> GetEmail(CancellationToken ct)
    {
        var s = await GetOrCreateSettingsAsync(ct);
        return Ok(new EmailSettingsDto(s.ImapHost, s.ImapPort, s.ImapSsl, s.ImapUsername, s.ImapPassword));
    }

    /// <summary>Updates the IMAP email configuration. Omit password to keep the current value.</summary>
    [HttpPut("email")]
    public async Task<IActionResult> PutEmail([FromBody] EmailSettingsDto dto, CancellationToken ct)
    {
        var s = await GetOrCreateSettingsAsync(ct);
        s.ImapHost     = dto.ImapHost;
        s.ImapPort     = dto.ImapPort;
        s.ImapSsl      = dto.ImapSsl;
        s.ImapUsername = dto.ImapUsername;
        if (!string.IsNullOrEmpty(dto.ImapPassword))
            s.ImapPassword = dto.ImapPassword;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Notifications ────────────────────────────────────────────────────────

    /// <summary>Returns the stored notification preferences.</summary>
    [HttpGet("notifications")]
    public async Task<ActionResult<NotificationSettingsDto>> GetNotifications(CancellationToken ct)
    {
        var s = await GetOrCreateSettingsAsync(ct);
        return Ok(new NotificationSettingsDto(
            s.NotifyNewTask, s.NotifyDeadlineToday, s.NotifyDailyNewsSummary, s.NewsSummaryHour));
    }

    /// <summary>Updates the notification preferences.</summary>
    [HttpPut("notifications")]
    public async Task<IActionResult> PutNotifications([FromBody] NotificationSettingsDto dto, CancellationToken ct)
    {
        var s = await GetOrCreateSettingsAsync(ct);
        s.NotifyNewTask          = dto.NotifyNewTask;
        s.NotifyDeadlineToday    = dto.NotifyDeadlineToday;
        s.NotifyDailyNewsSummary = dto.NotifyDailyNewsSummary;
        s.NewsSummaryHour        = Math.Clamp(dto.NewsSummaryHour, 0, 23);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── RSS Feeds ─────────────────────────────────────────────────────────────

    /// <summary>Returns all configured RSS feeds.</summary>
    [HttpGet("feeds")]
    public async Task<ActionResult<IEnumerable<RssFeedDto>>> GetFeeds(CancellationToken ct)
    {
        var feeds = await _db.RssFeeds.OrderBy(f => f.Id).ToListAsync(ct);
        return Ok(feeds.Select(f => new RssFeedDto(f.Id, f.Url, f.Category, f.Enabled)));
    }

    /// <summary>Adds a new RSS feed.</summary>
    [HttpPost("feeds")]
    public async Task<ActionResult<RssFeedDto>> AddFeed([FromBody] RssFeedCreateDto dto, CancellationToken ct)
    {
        var feed = new RssFeed { Url = dto.Url.Trim(), Category = dto.Category.Trim(), Enabled = true };
        _db.RssFeeds.Add(feed);
        await _db.SaveChangesAsync(ct);
        return Ok(new RssFeedDto(feed.Id, feed.Url, feed.Category, feed.Enabled));
    }

    /// <summary>Enables or disables an RSS feed.</summary>
    [HttpPut("feeds/{id:int}")]
    public async Task<IActionResult> UpdateFeed(int id, [FromBody] FeedUpdateDto dto, CancellationToken ct)
    {
        var feed = await _db.RssFeeds.FindAsync(new object[] { id }, ct);
        if (feed is null) return NotFound();
        feed.Enabled = dto.Enabled;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Deletes an RSS feed.</summary>
    [HttpDelete("feeds/{id:int}")]
    public async Task<IActionResult> DeleteFeed(int id, CancellationToken ct)
    {
        var feed = await _db.RssFeeds.FindAsync(new object[] { id }, ct);
        if (feed is null) return NotFound();
        _db.RssFeeds.Remove(feed);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
