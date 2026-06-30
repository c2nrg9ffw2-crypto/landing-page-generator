using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCH.Core.Interfaces;
using PCH.Core.Models;
using PCH.Data;

namespace PCH.Api.Controllers;

/// <summary>
/// Dev-only endpoints for manually triggering notifications without waiting
/// for the 5-minute background poll. Remove or gate behind auth before deploying.
/// </summary>
[ApiController]
[Route("api/debug/notifications")]
public class NotificationDebugController : ControllerBase
{
    private readonly INotificationService _notifications;
    private readonly PchDbContext _db;

    /// <summary>Creates the controller.</summary>
    public NotificationDebugController(INotificationService notifications, PchDbContext db)
    {
        _notifications = notifications;
        _db = db;
    }

    /// <summary>Fires a new-task toast with a synthetic task — no DB write.</summary>
    [HttpPost("test-new-task")]
    public IActionResult TestNewTask()
    {
        _notifications.NotifyNewTask(new TaskItem { Id = 0, Title = "Test Task (manual trigger)" });
        return Ok(new { fired = true, title = "Test Task (manual trigger)" });
    }

    /// <summary>
    /// Runs the deadline check immediately (same logic as the background service).
    /// Sends a toast for every incomplete task due today that hasn't been notified yet.
    /// </summary>
    [HttpPost("force-deadline-check")]
    public async Task<IActionResult> ForceDeadlineCheck(CancellationToken ct)
    {
        var all = await _db.Tasks.ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var dueToday = all
            .Where(t => t.DueDate.HasValue
                     && DateOnly.FromDateTime(t.DueDate.Value.LocalDateTime) == today
                     && t.Status != TaskState.Done
                     && !t.DeadlineNotified)
            .ToList();

        foreach (var task in dueToday)
        {
            _notifications.NotifyDeadlineToday(task);
            task.DeadlineNotified = true;
        }

        if (dueToday.Count > 0)
            await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            tasksNotified = dueToday.Count,
            titles = dueToday.Select(t => t.Title)
        });
    }

    /// <summary>
    /// Fires the daily news-summary toast immediately using the last-24-hour article count.
    /// Bypasses the hour check used by the background service.
    /// </summary>
    [HttpPost("force-news-summary")]
    public async Task<IActionResult> ForceNewsSummary(CancellationToken ct)
    {
        var all = await _db.NewsItems.ToListAsync(ct);
        var since = DateTime.UtcNow.AddHours(-24);
        var count = all.Count(n => n.FetchedAt.UtcDateTime >= since);

        _notifications.NotifyDailyNewsSummary(count);

        return Ok(new { articleCount = count, fired = true });
    }
}
