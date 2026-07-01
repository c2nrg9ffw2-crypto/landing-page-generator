using Microsoft.EntityFrameworkCore;
using PCH.Core.Interfaces;
using PCH.Core.Models;
using PCH.Data;

namespace PCH.Api.Services;

/// <summary>
/// Background service that polls every 5 minutes and:
/// 1. Sends a deadline-today toast for any incomplete task due today (once per task per day).
/// 2. Sends a morning news-summary toast once per day at the configured hour (default 08:00).
/// </summary>
public sealed class NotificationCheckService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notifications;
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationCheckService> _logger;

    // Tracks the date of the last news-summary toast so it fires exactly once per day.
    private DateOnly _lastNewsSummaryDate = DateOnly.MinValue;

    /// <summary>Creates the service with its dependencies.</summary>
    public NotificationCheckService(
        IServiceScopeFactory scopeFactory,
        INotificationService notifications,
        IConfiguration config,
        ILogger<NotificationCheckService> logger)
    {
        _scopeFactory   = scopeFactory;
        _notifications  = notifications;
        _config         = config;
        _logger         = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Short initial delay so the app finishes starting before the first check.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDeadlinesAsync(stoppingToken);
                await MaybeNotifyNewsSummaryAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationCheckService encountered an error");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task CheckDeadlinesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PchDbContext>();

        // Materialize first — SQLite cannot ORDER/filter DateTimeOffset columns reliably.
        var all = await db.Tasks.ToListAsync(ct);
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
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Sent deadline notification(s) for {Count} task(s)", dueToday.Count);
        }
    }

    private async Task MaybeNotifyNewsSummaryAsync(CancellationToken ct)
    {
        var summaryHour = _config.GetValue<int>("Notifications:NewsSummaryHour", 8);
        var now   = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        if (now.Hour != summaryHour || _lastNewsSummaryDate >= today)
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PchDbContext>();

        // Count articles fetched in the last 24 hours (materialize to avoid DateTimeOffset WHERE quirks).
        var all   = await db.NewsItems.ToListAsync(ct);
        var since = DateTime.UtcNow.AddHours(-24);
        var count = all.Count(n => n.FetchedAt.UtcDateTime >= since);

        _notifications.NotifyDailyNewsSummary(count);
        _lastNewsSummaryDate = today;

        _logger.LogInformation("Sent morning news summary: {Count} article(s) in last 24 h", count);
    }
}
