using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PCH.Core.Interfaces;
using PCH.Core.Models;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace PCH.Notifications;

/// <summary>
/// Sends Windows toast notifications using the WinRT APIs built into
/// <c>net8.0-windows10.0.17763.0</c>. No COM activator or third-party package required.
///
/// Requires an interactive desktop session — will silently no-op if the process
/// runs as a Windows Service. Run PCH.Api via <c>dotnet run</c> on the Beelink desktop.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private const string AppId   = "com.personalcommandhub.app";
    private const string AppName = "Personal Command Hub";

    private static bool _initialized;
    private static readonly object _initLock = new();

    private readonly ILogger<NotificationService> _logger;

    /// <summary>Creates the service and performs one-time per-user AUMID registry registration.</summary>
    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        lock (_initLock)
        {
            if (_initialized) return;
            try
            {
                // Windows 10 1903+ honours HKCU\SOFTWARE\Classes\AppUserModelId\{id}
                // without requiring a Start Menu shortcut or COM server registration.
                using var key = Registry.CurrentUser.CreateSubKey(
                    $@"SOFTWARE\Classes\AppUserModelId\{AppId}");
                key?.SetValue("DisplayName", AppName);

                _initialized = true;
                _logger.LogInformation("Toast AUMID registered: {AppId}", AppId);
            }
            catch (Exception ex)
            {
                // If AUMID registration fails, CreateToastNotifier will throw later.
                // Most likely cause: no write access to HKCU (unusual) or running headless.
                _logger.LogWarning(ex,
                    "Toast AUMID registration failed — notifications will be skipped. " +
                    "Ensure PCH.Api runs in an interactive desktop session, not as a Windows Service.");
            }
        }
    }

    /// <inheritdoc/>
    public void NotifyNewTask(TaskItem task) =>
        ShowToast("New Task Created", task.Title);

    /// <inheritdoc/>
    public void NotifyDeadlineToday(TaskItem task) =>
        ShowToast("Task Due Today", task.Title);

    /// <inheritdoc/>
    public void NotifyDailyNewsSummary(int articleCount) =>
        ShowToast("Morning News Summary", $"{articleCount} article(s) ready — open PCH to read them");

    private void ShowToast(string title, string body)
    {
        if (!_initialized)
        {
            _logger.LogDebug("Skipping toast '{Title}' — AUMID not registered", title);
            return;
        }

        try
        {
            var xml = $"""
                <toast>
                  <visual>
                    <binding template="ToastGeneric">
                      <text>{Escape(title)}</text>
                      <text>{Escape(body)}</text>
                    </binding>
                  </visual>
                </toast>
                """;

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            ToastNotificationManager.CreateToastNotifier(AppId).Show(new ToastNotification(doc));
        }
        catch (Exception ex)
        {
            // If toasts still don't appear after AUMID registration succeeded, the most
            // likely cause is Focus Assist / Do Not Disturb being active, or the process
            // lacking a desktop window station (Windows Service scenario).
            _logger.LogWarning(ex, "Failed to show toast '{Title}'", title);
        }
    }

    private static string Escape(string text) =>
        System.Security.SecurityElement.Escape(text) ?? text;
}
