using System.Net.Http.Json;
using PCH.Core.Dtos;

namespace PCH.App.Services;

/// <summary>
/// Typed HTTP client for the PCH Emails API (<c>/api/emails</c>).
/// </summary>
public class EmailApiClient
{
    private readonly HttpClient _http;

    /// <summary>Initialises the client with the configured <see cref="HttpClient"/>.</summary>
    public EmailApiClient(HttpClient http) => _http = http;

    /// <summary>Returns all stored emails, newest first.</summary>
    /// <param name="ct">Optional cancellation token.</param>
    public async Task<EmailResponseDto[]> GetEmailsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<EmailResponseDto[]>("api/emails", ct)
           ?? Array.Empty<EmailResponseDto>();

    /// <summary>
    /// Triggers an IMAP sync on the API side.
    /// Returns the number of new emails ingested, or throws on HTTP error.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    public async Task<int> SyncAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/emails/sync", content: null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SyncResult>(cancellationToken: ct);
        return result?.NewEmails ?? 0;
    }

    private sealed record SyncResult(int NewEmails);
}
