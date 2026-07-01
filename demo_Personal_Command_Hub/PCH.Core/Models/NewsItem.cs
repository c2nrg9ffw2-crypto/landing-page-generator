namespace PCH.Core.Models;

/// <summary>
/// A single news article pulled from an RSS feed.
/// Persisted to the <c>news_items</c> table.
/// </summary>
public class NewsItem
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Feed grouping shown on the dashboard (e.g. "World", "Tech").</summary>
    public string FeedCategory { get; set; } = string.Empty;

    /// <summary>Article headline.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Canonical article URL, also used to deduplicate.</summary>
    public string Link { get; set; } = string.Empty;

    /// <summary>Short summary or excerpt.</summary>
    public string? Summary { get; set; }

    /// <summary>Publication timestamp reported by the feed.</summary>
    public DateTimeOffset Published { get; set; }

    /// <summary>UTC timestamp when this item was fetched into PCH.</summary>
    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
}
