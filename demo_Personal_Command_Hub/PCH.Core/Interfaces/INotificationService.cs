using PCH.Core.Models;

namespace PCH.Core.Interfaces;

/// <summary>
/// Contract for sending Windows desktop toast notifications.
/// Implemented in PCH.Notifications; registered as singleton in PCH.Api.
/// </summary>
public interface INotificationService
{
    /// <summary>Fires a "New Task Created" toast with the task title.</summary>
    /// <param name="task">The newly created task.</param>
    void NotifyNewTask(TaskItem task);

    /// <summary>Fires a "Task Due Today" toast for tasks whose deadline falls on the current day.</summary>
    /// <param name="task">The task due today.</param>
    void NotifyDeadlineToday(TaskItem task);

    /// <summary>Fires the morning news-summary toast.</summary>
    /// <param name="articleCount">Number of articles fetched in the last 24 hours.</param>
    void NotifyDailyNewsSummary(int articleCount);
}
