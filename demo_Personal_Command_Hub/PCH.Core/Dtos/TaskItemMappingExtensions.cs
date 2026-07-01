using PCH.Core.Models;

namespace PCH.Core.Dtos;

/// <summary>Mapping helpers between <see cref="TaskItem"/> entities and task DTOs.</summary>
public static class TaskItemMappingExtensions
{
    /// <summary>Projects a <see cref="TaskItem"/> entity to a <see cref="TaskResponseDto"/>.</summary>
    /// <param name="task">The entity to map.</param>
    /// <returns>A response DTO mirroring the entity's current state.</returns>
    public static TaskResponseDto ToResponseDto(this TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Category,
        task.Status,
        task.Progress,
        task.DueDate,
        task.Source,
        task.CreatedAt,
        task.UpdatedAt);
}
