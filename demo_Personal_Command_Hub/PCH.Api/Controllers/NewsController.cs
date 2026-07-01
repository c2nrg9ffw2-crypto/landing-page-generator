using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCH.Connectors;
using PCH.Core.Dtos;
using PCH.Data;

namespace PCH.Api.Controllers;

/// <summary>
/// Endpoints for reading stored news articles and triggering an RSS sync.
/// </summary>
[ApiController]
[Route("api/news")]
public class NewsController : ControllerBase
{
    private readonly PchDbContext _db;
    private readonly RssConnector _connector;

    /// <summary>Creates the controller.</summary>
    /// <param name="db">The PCH database context.</param>
    /// <param name="connector">The RSS connector used to trigger a sync.</param>
    public NewsController(PchDbContext db, RssConnector connector)
    {
        _db = db;
        _connector = connector;
    }

    /// <summary>
    /// Returns stored news articles, optionally filtered by <paramref name="category"/>, newest first.
    /// SQLite cannot ORDER BY DateTimeOffset, so sorting is done in memory after materialising.
    /// </summary>
    /// <param name="category">Optional category filter (Sweden, Germany, Science).</param>
    /// <param name="ct">Request cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NewsResponseDto>>> GetAll(
        [FromQuery] string? category, CancellationToken ct)
    {
        var items = await _db.NewsItems.ToListAsync(ct);

        var query = items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(n => n.FeedCategory.Equals(category, StringComparison.OrdinalIgnoreCase));

        return Ok(query
            .OrderByDescending(n => n.Published)
            .Select(n => new NewsResponseDto(
                n.Id, n.FeedCategory, n.Title, n.Link, n.Summary, n.Published, n.FetchedAt)));
    }

    /// <summary>Triggers a fresh fetch across all configured RSS feeds.</summary>
    /// <param name="ct">Request cancellation token.</param>
    [HttpPost("sync")]
    public async Task<ActionResult<object>> Sync(CancellationToken ct)
    {
        var count = await _connector.FetchAsync(ct);
        return Ok(new { newItems = count });
    }
}
