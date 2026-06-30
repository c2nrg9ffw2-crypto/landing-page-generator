using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCH.Core.Dtos;
using PCH.Core.Interfaces;
using PCH.Core.Models;
using PCH.Data;

namespace PCH.Api.Controllers;

/// <summary>
/// CRUD endpoints for dashboard tasks, backed by the SQLite <c>tasks</c> table.
/// </summary>
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly PchDbContext _db;
    private readonly INotificationService _notifications;

    /// <summary>Creates the controller with the injected EF Core context and notification service.</summary>
    /// <param name="db">The PCH database context.</param>
    /// <param name="notifications">Service used to fire desktop toast notifications.</param>
    public TasksController(PchDbContext db, INotificationService notifications)
    {
        _db            = db;
        _notifications = notifications;
    }

    /// <summary>Returns all tasks, ordered by status, then due date (nulls last), then creation time.</summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The full list of tasks as response DTOs.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        // SQLite cannot ORDER BY DateTimeOffset, so materialize first and
        // order in memory (LINQ to Objects).
        var tasks = await _db.Tasks.ToListAsync(cancellationToken);

        var ordered = tasks
            .OrderBy(t => t.Status)
            .ThenBy(t => t.DueDate ?? DateTimeOffset.MaxValue)
            .ThenBy(t => t.CreatedAt);

        return Ok(ordered.Select(t => t.ToResponseDto()));
    }

    /// <summary>Returns a single task by id.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The task, or <c>404</c> if it does not exist.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return task is null ? NotFound() : Ok(task.ToResponseDto());
    }

    /// <summary>Creates a new manual task.</summary>
    /// <param name="dto">The creation payload.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns><c>201 Created</c> with the created task.</returns>
    [HttpPost]
    public async Task<ActionResult<TaskResponseDto>> Create(TaskCreateDto dto, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            DueDate = dto.DueDate,
            Progress = dto.Progress,
            Source = ItemSource.Manual,
            Status = TaskState.Todo,
            CreatedAt = now,
            UpdatedAt = now
        };

        ApplyProgressStatusSync(task);

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        _notifications.NotifyNewTask(task);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task.ToResponseDto());
    }

    /// <summary>Updates an existing task.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="dto">The update payload.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The updated task, or <c>404</c> if it does not exist.</returns>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskResponseDto>> Update(int id, TaskUpdateDto dto, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
            return NotFound();

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Category = dto.Category;
        task.Status = dto.Status;
        task.Progress = dto.Progress;
        task.DueDate = dto.DueDate;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        ApplyProgressStatusSync(task);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(task.ToResponseDto());
    }

    /// <summary>Deletes a task.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns><c>204 No Content</c>, or <c>404</c> if it does not exist.</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
            return NotFound();

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Keeps progress and status consistent: progress is clamped to 0–100;
    /// reaching 100 auto-completes the task (status <see cref="TaskState.Done"/>),
    /// and a task explicitly marked Done is forced to 100% progress.
    /// Centralized so create and update paths cannot diverge.
    /// </summary>
    /// <param name="task">The task whose progress/status to reconcile.</param>
    private static void ApplyProgressStatusSync(TaskItem task)
    {
        task.Progress = Math.Clamp(task.Progress, 0, 100);

        if (task.Progress >= 100)
        {
            task.Progress = 100;
            task.Status = TaskState.Done;
        }
        else if (task.Status == TaskState.Done && task.Progress < 100)
        {
            task.Progress = 100;
        }
    }
}
