using System.Net.Http.Json;
using PCH.Core.Dtos;

namespace PCH.App.Services;

/// <summary>
/// Typed HTTP client for the PCH Bookings API (<c>/api/bookings</c>).
/// </summary>
public class BookingApiClient
{
    private readonly HttpClient _http;

    /// <summary>Initialises the client with the configured <see cref="HttpClient"/>.</summary>
    public BookingApiClient(HttpClient http) => _http = http;

    /// <summary>Returns all stored bookings, soonest first.</summary>
    /// <param name="ct">Optional cancellation token.</param>
    public async Task<BookingResponseDto[]> GetBookingsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<BookingResponseDto[]>("api/bookings", ct)
        ?? Array.Empty<BookingResponseDto>();
}
