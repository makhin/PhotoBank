using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.Enrichment;

/// <summary>
/// Service for re-running enrichers on already processed photos
/// </summary>
public interface IReEnrichmentService
{
    /// <summary>
    /// Force re-run specific enrichers for a single photo, regardless of whether they were already applied.
    /// Use this when enricher algorithms have changed and you need to refresh existing enrichment data.
    /// </summary>
    /// <param name="photoId">Photo ID to re-enrich</param>
    /// <param name="enricherTypes">Specific enricher types to force re-run (with their dependencies)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if successful, false if photo not found or has no files</returns>
    /// <remarks>
    /// This method ALWAYS runs the specified enrichers, even if they have already been applied.
    /// For conditional re-enrichment (only missing enrichers), use <see cref="ReEnrichMissingAsync"/>.
    /// </remarks>
    Task<bool> ReEnrichPhotoAsync(int photoId, IReadOnlyCollection<Type> enricherTypes, CancellationToken ct = default);

    /// <summary>
    /// Force re-run specific enrichers for multiple photos, regardless of whether they were already applied.
    /// Use this when enricher algorithms have changed and you need to refresh existing enrichment data.
    /// </summary>
    /// <param name="photoIds">Photo IDs to re-enrich</param>
    /// <param name="enricherTypes">Specific enricher types to force re-run (with their dependencies)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of successfully re-enriched photos</returns>
    /// <remarks>
    /// This method ALWAYS runs the specified enrichers, even if they have already been applied.
    /// For conditional re-enrichment (only missing enrichers), use <see cref="ReEnrichMissingBatchAsync"/>.
    /// </remarks>
    Task<int> ReEnrichPhotosAsync(IReadOnlyCollection<int> photoIds, IReadOnlyCollection<Type> enricherTypes, CancellationToken ct = default);

    /// <summary>
    /// Re-run missing enrichers for a single photo based on current active enrichers
    /// </summary>
    /// <param name="photoId">Photo ID to check and re-enrich</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if successful, false if photo not found, has no files, or no enrichers needed</returns>
    Task<bool> ReEnrichMissingAsync(int photoId, CancellationToken ct = default);

    /// <summary>
    /// Re-run missing enrichers for multiple photos based on current active enrichers
    /// </summary>
    /// <param name="photoIds">Photo IDs to check and re-enrich</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of successfully re-enriched photos</returns>
    Task<int> ReEnrichMissingBatchAsync(IReadOnlyCollection<int> photoIds, CancellationToken ct = default);
}
