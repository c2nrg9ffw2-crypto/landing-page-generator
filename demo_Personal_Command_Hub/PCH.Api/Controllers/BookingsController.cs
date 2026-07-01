using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCH.Core.Dtos;
using PCH.Data;

namespace PCH.Api.Controllers;

/// <summary>
/// Read-only endpoint for stored bookings. Bookings are created by connectors
/// (currently the Zoezi detection in <see cref="PCH.Connectors.EmailConnector"/>), not via this API.
/// </summary>
[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly PchDbContext _db;

    /// <summary>Creates the controller with the injected EF Core context.</summary>
    /// <param name="db">The PCH database context.</param>
    public BookingsController(PchDbContext db) => _db = db;

    /// <summary>Returns all stored bookings, soonest first.</summary>
    /// <param name="ct">Request cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingResponseDto>>> GetAll(CancellationToken ct)
    {
        // SQLite cannot ORDER BY DateTimeOffset — sort in memory after materialising.
        var bookings = await _db.Bookings.ToListAsync(ct);
        return Ok(bookings
            .OrderBy(b => b.StartTime)
            .Select(b => new BookingResponseDto(
                b.Id, b.Title, b.Location, b.StartTime, b.EndTime, b.Source, b.Platform, b.CreatedAt)));
    }
}
