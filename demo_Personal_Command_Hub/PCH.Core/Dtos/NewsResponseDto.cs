namespace PCH.Core.Dtos;

/// <summary>
/// Represents a stored news article as returned by <c>GET /api/news</c>.
/// </summary>
/// <param name="Id">Database primary key.</param>
/// <param name="FeedCategory">Source feed label (e.g. "Sweden", "Germany", "Science").</param>
/// <param name="Title">Article headline.</param>
/// <param name="Link">Canonical article URL.</param>
/// <param name="Summary">Short excerpt from the feed, or null.</param>
/// <param name="Published">Publication timestamp reported by the feed.</param>
/// <param name="FetchedAt">UTC timestamp when PCH fetched and stored this item.</param>
public record NewsResponseDto(
    int Id,
    string FeedCategory,
    string Title,
    string Link,
    string? Summary,
    DateTimeOffset Published,
    DateTimeOffset FetchedAt);
