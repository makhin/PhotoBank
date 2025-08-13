using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichment;

public interface IEnrichmentPipeline
{
    /// <summary>Run all enrichers for a single item.</summary>
    Task RunAsync(Photo photo, SourceDataDto source, CancellationToken ct = default);

    /// <summary>Run pipeline for many items with optional parallelism.</summary>
    Task RunBatchAsync(IEnumerable<(Photo photo, SourceDataDto source)> items, CancellationToken ct = default);
}

