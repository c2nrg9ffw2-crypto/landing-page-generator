using System.ComponentModel.DataAnnotations;
using PCH.Core.Models;

namespace PCH.Core.Dtos;

/// <summary>
/// Payload for creating a new task. New tasks always originate as
/// <see cref="ItemSource.Manual"/> with status <see cref="TaskState.Todo"/>.
/// </summary>
/// <param name="Title">Required short title (max 256 chars).</param>
/// <param name="Description">Optional longer description.</param>
/// <param name="Category">Dashboard grouping.</param>
/// <param name="DueDate">Optional deadline.</param>
/// <param name="Progress">Initial completion percentage, 0–100 (defaults to 0).</param>
public record TaskCreateDto(
    [Required(AllowEmptyStrings = false, ErrorMessage = "Title is required.")]
    [StringLength(256)]
    string Title,
    string? Description,
    TaskCategory Category,
    DateTimeOffset? DueDate,
    int Progress = 0);
