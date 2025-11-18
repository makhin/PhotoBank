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
    /// Re-run specific enrichers for a single photo
    /// </summary>
    /// <param name="photoId">Photo ID to re-enrich</param>
    /// <param name="enricherTypes">Specific enricher types to re-run (with dependencies)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if successful, false if photo not found or has no files</returns>
    Task<bool> ReEnrichPhotoAsync(int photoId, IReadOnlyCollection<Type> enricherTypes, CancellationToken ct = default);

    /// <summary>
    /// Re-run specific enrichers for multiple photos
    /// </summary>
    /// <param name="photoIds">Photo IDs to re-enrich</param>
    /// <param name="enricherTypes">Specific enricher types to re-run (with dependencies)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of successfully re-enriched photos</returns>
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
