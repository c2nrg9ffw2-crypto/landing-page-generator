using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PCH.Core.Interfaces;
using PCH.Core.Models;
using PCH.Data;

namespace PCH.Connectors;

/// <summary>
/// Fetches the top 5 articles from each enabled RSS feed (stored in <c>rss_feeds</c>)
/// and persists them to <c>news_items</c>, deduplicating by URL.
/// </summary>
public partial class RssConnector : IConnector
{
    private readonly HttpClient _http;
    private readonly PchDbContext _db;
    private readonly ILogger<RssConnector> _logger;

    /// <inheritdoc />
    public string Name => "RSS";

    /// <summary>Initialises the connector.</summary>
    public RssConnector(HttpClient http, PchDbContext db, ILogger<RssConnector> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Fetches all enabled feeds from the database sequentially and persists new items.
    /// A feed that fails is logged and skipped so the others still complete.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total number of new articles stored across all feeds.</returns>
    public async Task<int> FetchAsync(CancellationToken cancellationToken = default)
    {
        var feeds = await _db.RssFeeds
            .Where(f => f.Enabled)
            .ToListAsync(cancellationToken);

        var total = 0;
        foreach (var feed in feeds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += await FetchFeedAsync(feed.Url, feed.Category, cancellationToken);
        }
        _logger.LogInformation("RSS sync complete: {Total} new article(s) ingested", total);
        return total;
    }

    private async Task<int> FetchFeedAsync(string url, string category, CancellationToken ct)
    {
        try
        {
            var xml = await _http.GetStringAsync(url, ct);

            SyndicationFeed feed;
            using (var reader = XmlReader.Create(
                new StringReader(xml),
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
            {
                feed = SyndicationFeed.Load(reader);
            }

            // Order by publish date in memory — SQLite DateTimeOffset ORDER BY is not supported.
            var top5 = feed.Items
                .OrderByDescending(i => i.PublishDate)
                .Take(5)
                .ToList();

            var newCount = 0;
            foreach (var item in top5)
            {
                ct.ThrowIfCancellationRequested();

                var link = item.Links.FirstOrDefault()?.Uri?.AbsoluteUri ?? string.Empty;
                if (string.IsNullOrWhiteSpace(link))
                    continue;

                if (await _db.NewsItems.AnyAsync(n => n.Link == link, ct))
                    continue;

                var rawSummary = item.Summary?.Text ?? string.Empty;
                var summary    = string.IsNullOrWhiteSpace(rawSummary)
                    ? null
                    : Truncate(StripHtml(rawSummary), 1000);

                _db.NewsItems.Add(new NewsItem
                {
                    FeedCategory = category,
                    Title        = Truncate(item.Title?.Text ?? "(no title)", 512),
                    Link         = link,
                    Summary      = summary,
                    Published    = item.PublishDate == default ? DateTimeOffset.UtcNow : item.PublishDate,
                    FetchedAt    = DateTimeOffset.UtcNow
                });

                await _db.SaveChangesAsync(ct);
                newCount++;
            }

            _logger.LogInformation("RSS {Category}: {New} new item(s) from {Url}", category, newCount, url);
            return newCount;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "RSS fetch failed for {Category} ({Url})", category, url);
            return 0;
        }
    }

    private static string StripHtml(string html) =>
        HtmlTagPattern().Replace(html, " ").Trim();

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max];

    [GeneratedRegex("<.*?>")]
    private static partial Regex HtmlTagPattern();
}
