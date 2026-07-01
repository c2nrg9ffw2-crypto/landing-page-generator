using System.ComponentModel.DataAnnotations;
using PCH.Core.Models;

namespace PCH.Core.Dtos;

/// <summary>
/// Payload for updating an existing task. The server applies the
/// progress/status consistency rule after binding (see TasksController).
/// </summary>
/// <param name="Title">Required short title (max 256 chars).</param>
/// <param name="Description">Optional longer description.</param>
/// <param name="Category">Dashboard grouping.</param>
/// <param name="Status">Desired lifecycle state.</param>
/// <param name="Progress">Completion percentage, 0–100.</param>
/// <param name="DueDate">Optional deadline.</param>
public record TaskUpdateDto(
    [Required(AllowEmptyStrings = false, ErrorMessage = "Title is required.")]
    [StringLength(256)]
    string Title,
    string? Description,
    TaskCategory Category,
    TaskState Status,
    int Progress,
    DateTimeOffset? DueDate);
