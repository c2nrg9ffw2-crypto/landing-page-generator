using System.Net.Http.Json;
using PCH.Core.Dtos;

namespace PCH.App.Services;

/// <summary>
/// Typed HTTP client for the PCH Settings API (<c>/api/settings</c>).
/// </summary>
public class SettingsApiClient
{
    private readonly HttpClient _http;

    /// <summary>Initialises the client with the configured <see cref="HttpClient"/>.</summary>
    public SettingsApiClient(HttpClient http) => _http = http;

    // ── Email ────────────────────────────────────────────────────────────────

    /// <summary>Returns the stored IMAP email configuration.</summary>
    public async Task<EmailSettingsDto?> GetEmailAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<EmailSettingsDto>("api/settings/email", ct);

    /// <summary>Saves the IMAP email configuration. Leave <c>ImapPassword</c> null to keep the current value.</summary>
    public async Task SaveEmailAsync(EmailSettingsDto dto, CancellationToken ct = default)
    {
        var r = await _http.PutAsJsonAsync("api/settings/email", dto, ct);
        r.EnsureSuccessStatusCode();
    }

    // ── Notifications ─────────────────────────────────────────────────────────

    /// <summary>Returns the stored notification preferences.</summary>
    public async Task<NotificationSettingsDto?> GetNotificationsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<NotificationSettingsDto>("api/settings/notifications", ct);

    /// <summary>Saves the notification preferences.</summary>
    public async Task SaveNotificationsAsync(NotificationSettingsDto dto, CancellationToken ct = default)
    {
        var r = await _http.PutAsJsonAsync("api/settings/notifications", dto, ct);
        r.EnsureSuccessStatusCode();
    }

    // ── RSS Feeds ─────────────────────────────────────────────────────────────

    /// <summary>Returns all configured RSS feeds.</summary>
    public async Task<RssFeedDto[]> GetFeedsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<RssFeedDto[]>("api/settings/feeds", ct) ?? [];

    /// <summary>Adds a new RSS feed.</summary>
    public async Task<RssFeedDto?> AddFeedAsync(RssFeedCreateDto dto, CancellationToken ct = default)
    {
        var r = await _http.PostAsJsonAsync("api/settings/feeds", dto, ct);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<RssFeedDto>(cancellationToken: ct);
    }

    /// <summary>Enables or disables an RSS feed.</summary>
    public async Task UpdateFeedAsync(int id, bool enabled, CancellationToken ct = default)
    {
        var r = await _http.PutAsJsonAsync($"api/settings/feeds/{id}", new FeedUpdateDto(enabled), ct);
        r.EnsureSuccessStatusCode();
    }

    /// <summary>Deletes an RSS feed.</summary>
    public async Task DeleteFeedAsync(int id, CancellationToken ct = default)
    {
        var r = await _http.DeleteAsync($"api/settings/feeds/{id}", ct);
        r.EnsureSuccessStatusCode();
    }
}
