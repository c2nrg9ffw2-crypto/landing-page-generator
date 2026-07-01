namespace PCH.Core.Models;

/// <summary>
/// Single-row settings table (Id always 1).
/// Stores IMAP email config and notification preferences.
/// </summary>
public class AppSettings
{
    public int Id { get; set; }

    // Email / IMAP
    public string ImapHost { get; set; } = string.Empty;
    public int ImapPort { get; set; } = 993;
    public bool ImapSsl { get; set; } = true;
    public string? ImapUsername { get; set; }
    public string? ImapPassword { get; set; }

    // Notifications
    public bool NotifyNewTask { get; set; } = true;
    public bool NotifyDeadlineToday { get; set; } = true;
    public bool NotifyDailyNewsSummary { get; set; } = true;
    public int NewsSummaryHour { get; set; } = 8;
}
