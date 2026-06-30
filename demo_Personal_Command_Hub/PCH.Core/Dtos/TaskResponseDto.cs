using PCH.Core.Models;

namespace PCH.Core.Dtos;

/// <summary>
/// Task representation returned by the API for both reads and write responses.
/// </summary>
/// <param name="Id">Primary key.</param>
/// <param name="Title">Short title.</param>
/// <param name="Description">Optional longer description.</param>
/// <param name="Category">Dashboard grouping.</param>
/// <param name="Status">Current lifecycle state.</param>
/// <param name="Progress">Completion percentage, 0–100.</param>
/// <param name="DueDate">Optional deadline.</param>
/// <param name="Source">How the task was created.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC timestamp of the last update.</param>
public record TaskResponseDto(
    int Id,
    string Title,
    string? Description,
    TaskCategory Category,
    TaskState Status,
    int Progress,
    DateTimeOffset? DueDate,
    ItemSource Source,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
