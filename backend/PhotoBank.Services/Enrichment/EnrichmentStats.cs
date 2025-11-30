using System.Collections.Generic;

namespace PhotoBank.Services.Enrichment;

/// <summary>
/// Statistics about enrichment pipeline execution.
/// </summary>
public sealed class EnrichmentStats
{
    /// <summary>
    /// Time taken by each enricher in milliseconds.
    /// Key: enricher type name, Value: execution time in ms.
    /// </summary>
    public Dictionary<string, long> EnricherTimes { get; } = new();

    /// <summary>
    /// Total enrichment time in milliseconds.
    /// </summary>
    public long TotalMilliseconds { get; set; }
}
