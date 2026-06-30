namespace PCH.Core.Models;

/// <summary>
/// A reservation/appointment, typically parsed from a booking confirmation
/// email. Persisted to the <c>bookings</c> table.
/// </summary>
public class Booking
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>What the booking is for (e.g. "Dentist", "Hotel — Oslo").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional venue or address.</summary>
    public string? Location { get; set; }

    /// <summary>Start of the booking.</summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>Optional end of the booking.</summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>Where this booking was ingested from.</summary>
    public ItemSource Source { get; set; } = ItemSource.Booking;

    /// <summary>
    /// Stable external identifier (e.g. confirmation number or email message-id)
    /// used to deduplicate.
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
