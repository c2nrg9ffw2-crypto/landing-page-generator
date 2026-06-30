using System.Net;
using System.Net.Http.Json;
using PCH.Core.Dtos;

namespace PCH.App.Services;

/// <summary>
/// Typed HTTP client for the PCH Tasks API.
/// </summary>
public class TaskApiClient
{
    private readonly HttpClient _http;

    /// <summary>Initialises the client with the configured <see cref="HttpClient"/>.</summary>
    /// <param name="http">The HTTP client (base address configured in DI).</param>
    public TaskApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Returns all tasks from the API.</summary>
    /// <param name="ct">Optional cancellation token.</param>
    public async Task<TaskResponseDto[]> GetTasksAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<TaskResponseDto[]>("api/tasks", ct)
               ?? Array.Empty<TaskResponseDto>();
    }

    /// <summary>Returns a single task by <paramref name="id"/>, or <c>null</c> if not found.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="ct">Optional cancellation token.</param>
    public async Task<TaskResponseDto?> GetTaskAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/tasks/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskResponseDto>(cancellationToken: ct);
    }

    /// <summary>Creates a new task and returns the created resource.</summary>
    /// <param name="dto">The creation payload.</param>
    /// <param name="ct">Optional cancellation token.</param>
    public async Task<TaskResponseDto?> CreateTaskAsync(TaskCreateDto dto, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/tasks", dto, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskResponseDto>(cancellationToken: ct);
    }

    /// <summary>Updates an existing task. Returns <c>true</c> on success, <c>false</c> if not found.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="dto">The update payload.</param>
    /// <param name="ct">Optional cancellation token.</param>
    public async Task<bool> UpdateTaskAsync(int id, TaskUpdateDto dto, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/tasks/{id}", dto, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;
        response.EnsureSuccessStatusCode();
        return true;
    }

    /// <summary>Deletes a task by <paramref name="id"/>. Returns <c>true</c> on success, <c>false</c> if not found.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="ct">Optional cancellation token.</param>
    public async Task<bool> DeleteTaskAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/tasks/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;
        response.EnsureSuccessStatusCode();
        return true;
    }
}
