using PCH.Core.Models;

namespace PCH.Core.Dtos;

/// <summary>
/// Represents a stored booking as returned by <c>GET /api/bookings</c>.
/// </summary>
/// <param name="Id">Database primary key.</param>
/// <param name="Title">What the booking is for (e.g. a class name).</param>
/// <param name="Location">Optional venue or address.</param>
/// <param name="StartTime">Start of the booking.</param>
/// <param name="EndTime">Optional end of the booking.</param>
/// <param name="Source">Where this booking was ingested from.</param>
/// <param name="Platform">Name of the booking platform (e.g. "Zoezi"), or null.</param>
/// <param name="CreatedAt">UTC timestamp when this booking was stored.</param>
public record BookingResponseDto(
    int Id,
    string Title,
    string? Location,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    ItemSource Source,
    string? Platform,
    DateTimeOffset CreatedAt);
