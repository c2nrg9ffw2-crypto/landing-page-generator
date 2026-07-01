namespace PCH.Core.Models;

/// <summary>An RSS feed configured for news sync. Persisted to <c>rss_feeds</c>.</summary>
public class RssFeed
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
