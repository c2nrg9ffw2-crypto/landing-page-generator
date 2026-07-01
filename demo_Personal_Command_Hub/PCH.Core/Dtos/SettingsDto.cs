namespace PCH.Core.Dtos;

public record EmailSettingsDto(
    string ImapHost,
    int ImapPort,
    bool ImapSsl,
    string? ImapUsername,
    string? ImapPassword);

public record NotificationSettingsDto(
    bool NotifyNewTask,
    bool NotifyDeadlineToday,
    bool NotifyDailyNewsSummary,
    int NewsSummaryHour);

public record RssFeedDto(int Id, string Url, string Category, bool Enabled);

public record RssFeedCreateDto(string Url, string Category);

public record FeedUpdateDto(bool Enabled);
