using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichment;

/// <summary>
/// Result of enrichment pipeline execution.
/// </summary>
public sealed record EnrichmentResult(string? StopReason, EnrichmentStats Stats);

public interface IEnrichmentPipeline
{
    /// <summary>
    /// Run all enrichers for a single item.
    /// Returns enrichment result with stop reason (if stopped early) and execution statistics.
    /// </summary>
    Task<EnrichmentResult> RunAsync(Photo photo, SourceDataDto source, CancellationToken ct = default);

    /// <summary>
    /// Run a subset of enrichers for a single item.
    /// Returns enrichment result with stop reason (if stopped early) and execution statistics.
    /// </summary>
    Task<EnrichmentResult> RunAsync(Photo photo, SourceDataDto source, IReadOnlyCollection<Type> enrichers, CancellationToken ct = default);

    /// <summary>
    /// Run a subset of enrichers for a single item using a specific service provider.
    /// When serviceProvider is provided, enrichers are resolved from it instead of creating a new scope.
    /// This allows enrichers to participate in the caller's transaction by sharing the same DbContext instance.
    /// Returns enrichment result with stop reason (if stopped early) and execution statistics.
    /// </summary>
    Task<EnrichmentResult> RunAsync(Photo photo, SourceDataDto source, IReadOnlyCollection<Type> enrichers, IServiceProvider? serviceProvider, CancellationToken ct = default);

    /// <summary>Run pipeline for many items with optional parallelism.</summary>
    Task RunBatchAsync(IEnumerable<(Photo photo, SourceDataDto source)> items, CancellationToken ct = default);
}

