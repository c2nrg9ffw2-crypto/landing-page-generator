using System.Net.Http.Json;
using PCH.Core.Dtos;

namespace PCH.App.Services;

/// <summary>
/// Typed HTTP client for the PCH News API (<c>/api/news</c>).
/// </summary>
public class NewsApiClient
{
    private readonly HttpClient _http;

    /// <summary>Initialises the client with the configured <see cref="HttpClient"/>.</summary>
    public NewsApiClient(HttpClient http) => _http = http;

    /// <summary>
    /// Returns stored news articles, optionally filtered to one feed category, newest first.
    /// </summary>
    /// <param name="category">Optional category filter (Sweden, Germany, Science). Null returns all.</param>
    /// <param name="ct">Optional cancellation token.</param>
    public async Task<NewsResponseDto[]> GetNewsAsync(string? category = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(category)
            ? "api/news"
            : $"api/news?category={Uri.EscapeDataString(category)}";
        return await _http.GetFromJsonAsync<NewsResponseDto[]>(url, ct)
               ?? Array.Empty<NewsResponseDto>();
    }

    /// <summary>Triggers an RSS sync on the API. Returns the number of new articles stored.</summary>
    /// <param name="ct">Optional cancellation token.</param>
    public async Task<int> SyncAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/news/sync", content: null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SyncResult>(cancellationToken: ct);
        return result?.NewItems ?? 0;
    }

    private sealed record SyncResult(int NewItems);
}
