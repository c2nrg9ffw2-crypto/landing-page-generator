namespace PCH.Core.Interfaces;

/// <summary>
/// Contract for every external data source connector (email, RSS, booking, …).
/// Implementations fetch from an external system and persist the results as
/// PCH domain entities.
/// </summary>
public interface IConnector
{
    /// <summary>Human-readable connector name, used for logging (e.g. "Email", "RSS").</summary>
    string Name { get; }

    /// <summary>
    /// Fetches the latest data from the external source and persists it.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the fetch.</param>
    /// <returns>The number of new items ingested during this fetch.</returns>
    Task<int> FetchAsync(CancellationToken cancellationToken = default);
}
