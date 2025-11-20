using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichment;

/// <summary>
/// Service for re-running enrichers on already processed photos
/// </summary>
public sealed class ReEnrichmentService : IReEnrichmentService
{
    private readonly PhotoBankDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRepository<Enricher> _enricherRepository;
    private readonly IEnrichmentPipeline _enrichmentPipeline;
    private readonly IActiveEnricherProvider _activeEnricherProvider;
    private readonly EnricherDiffCalculator _enricherDiffCalculator;
    private readonly ILogger<ReEnrichmentService> _logger;

    public ReEnrichmentService(
        PhotoBankDbContext context,
        IServiceProvider serviceProvider,
        IRepository<Enricher> enricherRepository,
        IEnrichmentPipeline enrichmentPipeline,
        IActiveEnricherProvider activeEnricherProvider,
        EnricherDiffCalculator enricherDiffCalculator,
        ILogger<ReEnrichmentService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _enricherRepository = enricherRepository ?? throw new ArgumentNullException(nameof(enricherRepository));
        _enrichmentPipeline = enrichmentPipeline ?? throw new ArgumentNullException(nameof(enrichmentPipeline));
        _activeEnricherProvider = activeEnricherProvider ?? throw new ArgumentNullException(nameof(activeEnricherProvider));
        _enricherDiffCalculator = enricherDiffCalculator ?? throw new ArgumentNullException(nameof(enricherDiffCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ReEnrichPhotoAsync(int photoId, IReadOnlyCollection<Type> enricherTypes, CancellationToken ct = default)
    {
        if (enricherTypes == null || !enricherTypes.Any())
        {
            _logger.LogWarning("No enricher types specified for re-enrichment of photo {PhotoId}", photoId);
            return false;
        }

        var photo = await LoadPhotoWithDependenciesAsync(photoId, ct);
        if (photo == null)
        {
            _logger.LogWarning("Photo {PhotoId} not found", photoId);
            return false;
        }

        var absolutePath = GetPhotoAbsolutePath(photo);
        if (absolutePath == null)
        {
            _logger.LogWarning("Photo {PhotoId} has no files or file path does not exist", photoId);
            return false;
        }

        // Start a database transaction to ensure atomicity.
        // Enrichers (like BaseLookupEnricher) may call repository.InsertAsync() which commits immediately.
        // Without a transaction, if flag verification fails, photo changes are rolled back via ChangeTracker.Clear()
        // but lookup entities (tags/categories) inserted by enrichers remain orphaned in the database.
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            _logger.LogInformation("Force re-running {Count} enrichers for photo {PhotoId}: {Enrichers}",
                enricherTypes.Count, photoId, string.Join(", ", enricherTypes.Select(t => t.Name)));

            var sourceData = new SourceDataDto { AbsolutePath = absolutePath };

            // Expand specified enrichers with all their dependencies for EnrichmentPipeline
            // This is a FORCE re-run, so we don't filter by already-applied status
            var enrichersForPipeline = _enricherDiffCalculator.ExpandWithDependencies(enricherTypes);

            // Determine which EnricherType flags we expect to be set after successful enrichment
            var expectedFlags = EnricherType.None;
            foreach (var enricherType in enrichersForPipeline)
            {
                var enricher = _serviceProvider.GetService(enricherType) as Services.Enrichers.IEnricher;
                if (enricher != null)
                {
                    expectedFlags |= enricher.EnricherType;
                }
            }

            // Clear existing enrichment data for the enrichers being run to prevent duplicates
            ClearEnrichmentData(photo, enrichersForPipeline);

            // Run enrichment pipeline with all specified enrichers and their dependencies
            await _enrichmentPipeline.RunAsync(photo, sourceData, enrichersForPipeline, ct);

            // Verify all enrichers succeeded by checking if they set their flags
            // With ContinueOnError=true, the pipeline swallows exceptions but enrichers won't set flags if they fail
            var actualFlags = photo.EnrichedWithEnricherType & expectedFlags;
            if (actualFlags != expectedFlags)
            {
                var missingFlags = expectedFlags & ~actualFlags;
                _logger.LogWarning(
                    "Some enrichers failed for photo {PhotoId} (expected flags: {Expected}, actual: {Actual}, missing: {Missing}). " +
                    "Rolling back transaction to prevent partial state.",
                    photoId, expectedFlags, actualFlags, missingFlags);

                // Rollback transaction - this discards database changes including lookup entities
                // inserted by enrichers (tags/categories via BaseLookupEnricher.InsertAsync)
                await transaction.RollbackAsync(ct);

                // Clear change tracker - rollback discards database changes but tracked entities
                // remain in memory. In batch operations, the same DbContext is reused for the next
                // photo, and SaveChangesAsync would persist these rolled-back changes.
                _context.ChangeTracker.Clear();

                return false;
            }

            // Save changes and commit transaction
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Successfully re-enriched photo {PhotoId} with {Count} enrichers",
                photoId, enrichersForPipeline.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to re-enrich photo {PhotoId}, rolling back transaction", photoId);

            // Rollback transaction on exception - this discards database changes including
            // lookup entities inserted by enrichers
            await transaction.RollbackAsync(ct);

            // Clear change tracker - rollback discards database changes but tracked entities
            // remain in memory. In batch operations, the same DbContext is reused for the next
            // photo, and SaveChangesAsync would persist these rolled-back changes.
            _context.ChangeTracker.Clear();

            throw;
        }
    }

    public async Task<int> ReEnrichPhotosAsync(IReadOnlyCollection<int> photoIds, IReadOnlyCollection<Type> enricherTypes, CancellationToken ct = default)
    {
        if (photoIds == null || !photoIds.Any())
        {
            _logger.LogWarning("No photo IDs specified for re-enrichment");
            return 0;
        }

        if (enricherTypes == null || !enricherTypes.Any())
        {
            _logger.LogWarning("No enricher types specified for re-enrichment");
            return 0;
        }

        _logger.LogInformation("Starting batch re-enrichment for {Count} photos with enrichers: {Enrichers}",
            photoIds.Count, string.Join(", ", enricherTypes.Select(t => t.Name)));

        var successCount = 0;
        foreach (var photoId in photoIds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var success = await ReEnrichPhotoAsync(photoId, enricherTypes, ct);
                if (success)
                {
                    successCount++;
                }
            }
            catch (OperationCanceledException)
            {
                // Propagate cancellation to caller
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-enrich photo {PhotoId}, continuing with next photo", photoId);

                // Clear change tracker for safety. The transaction in ReEnrichPhotoAsync was already
                // rolled back, but clearing the tracker ensures we start fresh for the next photo.
                _context.ChangeTracker.Clear();

                // Continue with other photos
            }
        }

        _logger.LogInformation("Batch re-enrichment completed: {SuccessCount}/{TotalCount} photos successfully processed",
            successCount, photoIds.Count);

        return successCount;
    }

    public async Task<bool> ReEnrichMissingAsync(int photoId, CancellationToken ct = default)
    {
        var photo = await LoadPhotoWithDependenciesAsync(photoId, ct);
        if (photo == null)
        {
            _logger.LogWarning("Photo {PhotoId} not found", photoId);
            return false;
        }

        var absolutePath = GetPhotoAbsolutePath(photo);
        if (absolutePath == null)
        {
            _logger.LogWarning("Photo {PhotoId} has no files or file path does not exist", photoId);
            return false;
        }

        // Get currently active enrichers from configuration
        var activeEnrichers = _activeEnricherProvider.GetActiveEnricherTypes(_enricherRepository);
        if (!activeEnrichers.Any())
        {
            _logger.LogWarning("No active enrichers configured");
            return false;
        }

        // Filter to only include enrichers that are actually registered in the DI container
        // In API, only core enrichers (Metadata, Thumbnail, Preview) are registered
        // In Console, all enrichers are registered
        var registeredEnrichers = activeEnrichers
            .Where(enricherType =>
            {
                var isRegistered = _serviceProvider.GetService(enricherType) != null;
                if (!isRegistered)
                {
                    _logger.LogDebug("Skipping unregistered enricher {EnricherType} for photo {PhotoId}",
                        enricherType.Name, photoId);
                }
                return isRegistered;
            })
            .ToArray();

        if (!registeredEnrichers.Any())
        {
            _logger.LogWarning("No registered enrichers found among active enrichers for photo {PhotoId}", photoId);
            return false;
        }

        // Calculate missing enrichers based on what's already applied
        var missingEnrichers = _enricherDiffCalculator.CalculateMissingEnrichers(photo, registeredEnrichers);

        if (!missingEnrichers.Any())
        {
            _logger.LogDebug("Photo {PhotoId} has all active enrichers already applied", photoId);
            return false; // Nothing to do
        }

        // Start a database transaction to ensure atomicity.
        // Enrichers (like BaseLookupEnricher) may call repository.InsertAsync() which commits immediately.
        // Without a transaction, if flag verification fails, photo changes are rolled back via ChangeTracker.Clear()
        // but lookup entities (tags/categories) inserted by enrichers remain orphaned in the database.
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            _logger.LogInformation("Re-enriching photo {PhotoId} with {Count} missing enrichers: {Enrichers}",
                photoId, missingEnrichers.Count, string.Join(", ", missingEnrichers.Select(t => t.Name)));

            var sourceData = new SourceDataDto { AbsolutePath = absolutePath };

            // Expand missing enrichers with all their dependencies for topological sorting.
            // The expanded list includes both missing enrichers AND their already-applied dependencies.
            var expandedEnrichers = _enricherDiffCalculator.ExpandWithDependencies(missingEnrichers);

            // Filter to identify enrichers that should have their data cleared.
            // We clear data for enrichers that are:
            //   1. In the missingEnrichers list (what the diff calculator wants to run), OR
            //   2. Not already applied (flag not set)
            //
            // This respects the diff calculator's decision about data-provider enrichers (Preview/Analyze).
            // Data providers may be returned by CalculateMissingEnrichers even if their flags are set,
            // because they populate SourceDataDto fields that need regeneration after algorithm changes.
            //
            // Example 1: Photo has Metadata but missing UnifiedFaceEnricher (depends on Metadata).
            //   - missingEnrichers = [UnifiedFaceEnricher]
            //   - expandedEnrichers = [Metadata, UnifiedFaceEnricher]
            //   - enrichersToClean = [UnifiedFaceEnricher] (Metadata already applied and not in missing list)
            //
            // Example 2: Photo has Preview already applied, but it's a data provider.
            //   - missingEnrichers = [Preview] (returned by CalculateMissingEnrichers for re-run)
            //   - expandedEnrichers = [Preview]
            //   - enrichersToClean = [Preview] (in missing list, so clear and re-run)
            var enrichersToClean = expandedEnrichers
                .Where(enricherType =>
                {
                    var enricher = _serviceProvider.GetService(enricherType) as Services.Enrichers.IEnricher;
                    if (enricher == null)
                    {
                        return false;
                    }

                    var isInMissingList = missingEnrichers.Contains(enricherType);
                    var isAlreadyApplied = photo.EnrichedWithEnricherType.HasFlag(enricher.EnricherType);

                    // Include if in missing list (diff calculator wants it) OR not already applied
                    if (isAlreadyApplied && !isInMissingList)
                    {
                        _logger.LogDebug(
                            "Not clearing already-applied dependency {EnricherType} for photo {PhotoId}",
                            enricherType.Name, photoId);
                        return false;
                    }

                    return true;
                })
                .ToArray();

            if (!enrichersToClean.Any())
            {
                _logger.LogDebug("Photo {PhotoId} has all required enrichers (including dependencies) already applied", photoId);
                return false;
            }

            // Determine which EnricherType flags we expect to be set after successful enrichment.
            // This includes ALL enrichers that will be run (both missing and already-applied dependencies).
            var expectedFlags = EnricherType.None;
            foreach (var enricherType in expandedEnrichers)
            {
                var enricher = _serviceProvider.GetService(enricherType) as Services.Enrichers.IEnricher;
                if (enricher != null)
                {
                    expectedFlags |= enricher.EnricherType;
                }
            }

            // Clear existing enrichment data ONLY for enrichers that are actually missing.
            // Already-applied dependencies are NOT cleared, so their data and flags remain intact.
            ClearEnrichmentData(photo, enrichersToClean);

            // Run enrichment pipeline with FULL expanded list (missing enrichers + dependencies).
            // TopoSort requires all dependencies to be present in the list for proper ordering.
            // Already-applied dependencies will re-run but won't overwrite cleared data (since we didn't clear them).
            await _enrichmentPipeline.RunAsync(photo, sourceData, expandedEnrichers, ct);

            // Verify all enrichers succeeded by checking if they set their flags
            // With ContinueOnError=true, the pipeline swallows exceptions but enrichers won't set flags if they fail
            var actualFlags = photo.EnrichedWithEnricherType & expectedFlags;
            if (actualFlags != expectedFlags)
            {
                var missingFlags = expectedFlags & ~actualFlags;
                _logger.LogWarning(
                    "Some enrichers failed for photo {PhotoId} (expected flags: {Expected}, actual: {Actual}, missing: {Missing}). " +
                    "Rolling back transaction to prevent partial state.",
                    photoId, expectedFlags, actualFlags, missingFlags);

                // Rollback transaction - this discards database changes including lookup entities
                // inserted by enrichers (tags/categories via BaseLookupEnricher.InsertAsync)
                await transaction.RollbackAsync(ct);

                // Clear change tracker - rollback discards database changes but tracked entities
                // remain in memory. In batch operations, the same DbContext is reused for the next
                // photo, and SaveChangesAsync would persist these rolled-back changes.
                _context.ChangeTracker.Clear();

                return false;
            }

            // Save changes and commit transaction
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Successfully re-enriched photo {PhotoId} with missing enrichers", photoId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to re-enrich photo {PhotoId} with missing enrichers, rolling back transaction", photoId);

            // Rollback transaction on exception - this discards database changes including
            // lookup entities inserted by enrichers
            await transaction.RollbackAsync(ct);

            // Clear change tracker - rollback discards database changes but tracked entities
            // remain in memory. In batch operations, the same DbContext is reused for the next
            // photo, and SaveChangesAsync would persist these rolled-back changes.
            _context.ChangeTracker.Clear();

            throw;
        }
    }

    public async Task<int> ReEnrichMissingBatchAsync(IReadOnlyCollection<int> photoIds, CancellationToken ct = default)
    {
        if (photoIds == null || !photoIds.Any())
        {
            _logger.LogWarning("No photo IDs specified for missing enrichers re-enrichment");
            return 0;
        }

        _logger.LogInformation("Starting batch re-enrichment for {Count} photos (missing enrichers only)", photoIds.Count);

        var successCount = 0;
        foreach (var photoId in photoIds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var success = await ReEnrichMissingAsync(photoId, ct);
                if (success)
                {
                    successCount++;
                }
            }
            catch (OperationCanceledException)
            {
                // Propagate cancellation to caller
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-enrich photo {PhotoId} with missing enrichers, continuing with next photo", photoId);

                // Clear change tracker for safety. The transaction in ReEnrichMissingAsync was already
                // rolled back, but clearing the tracker ensures we start fresh for the next photo.
                _context.ChangeTracker.Clear();

                // Continue with other photos
            }
        }

        _logger.LogInformation("Batch re-enrichment (missing) completed: {SuccessCount}/{TotalCount} photos successfully processed",
            successCount, photoIds.Count);

        return successCount;
    }

    /// <summary>
    /// Loads a photo with all necessary dependencies for re-enrichment.
    /// Uses tracked query so EF Core's change tracker only updates modified properties,
    /// avoiding the lost update problem where concurrent user edits would be overwritten.
    /// </summary>
    private async Task<Photo> LoadPhotoWithDependenciesAsync(int photoId, CancellationToken ct)
    {
        return await _context.Photos
            .Include(p => p.Storage)
            .Include(p => p.Files)
            .Include(p => p.Captions)
            .Include(p => p.PhotoTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PhotoCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.ObjectProperties)
            .Include(p => p.Faces)
            .FirstOrDefaultAsync(p => p.Id == photoId, ct);
    }

    /// <summary>
    /// Clears existing enrichment data for the specified enricher types to prevent duplicates.
    /// This method removes collection entries (captions, tags, faces, etc.) and clears the
    /// corresponding flags from EnrichedWithEnricherType so enrichers can repopulate fresh data.
    /// </summary>
    private void ClearEnrichmentData(Photo photo, IReadOnlyCollection<Type> enricherTypes)
    {
        // Determine which EnricherType flags are being re-run by instantiating enrichers
        var enricherTypeFlags = EnricherType.None;
        foreach (var enricherType in enricherTypes)
        {
            var enricher = _serviceProvider.GetService(enricherType) as Services.Enrichers.IEnricher;
            if (enricher != null)
            {
                enricherTypeFlags |= enricher.EnricherType;
            }
        }

        _logger.LogDebug("Clearing enrichment data for photo {PhotoId} with flags: {Flags}",
            photo.Id, enricherTypeFlags);

        // Clear collections based on which enricher types are running
        if (enricherTypeFlags.HasFlag(EnricherType.Caption) && photo.Captions != null)
        {
            _logger.LogDebug("Clearing {Count} captions for photo {PhotoId}", photo.Captions.Count, photo.Id);
            photo.Captions.Clear();
        }

        if (enricherTypeFlags.HasFlag(EnricherType.Tag) && photo.PhotoTags != null)
        {
            _logger.LogDebug("Clearing {Count} photo tags for photo {PhotoId}", photo.PhotoTags.Count, photo.Id);
            photo.PhotoTags.Clear();
        }

        if (enricherTypeFlags.HasFlag(EnricherType.Category) && photo.PhotoCategories != null)
        {
            _logger.LogDebug("Clearing {Count} categories for photo {PhotoId}", photo.PhotoCategories.Count, photo.Id);
            photo.PhotoCategories.Clear();
        }

        if (enricherTypeFlags.HasFlag(EnricherType.Face) && photo.Faces != null)
        {
            _logger.LogDebug("Clearing {Count} faces for photo {PhotoId}", photo.Faces.Count, photo.Id);
            photo.Faces.Clear();
        }

        if (enricherTypeFlags.HasFlag(EnricherType.ObjectProperty) && photo.ObjectProperties != null)
        {
            _logger.LogDebug("Clearing {Count} object properties for photo {PhotoId}", photo.ObjectProperties.Count, photo.Id);
            photo.ObjectProperties.Clear();
        }

        // Clear metadata fields when Metadata enricher is being re-run
        // Without this, MetadataEnricher's ??= operators would leave existing values untouched
        if (enricherTypeFlags.HasFlag(EnricherType.Metadata))
        {
            _logger.LogDebug("Clearing metadata fields for photo {PhotoId}", photo.Id);
            photo.Height = null;
            photo.Width = null;
            photo.Orientation = null;
            photo.TakenDate = null;
            photo.Location = null;
            photo.ImageHash = null;
            // Note: TakenMonth and TakenDay have private setters and are derived from TakenDate
        }

        // Clear adult content fields when Adult enricher is being re-run
        if (enricherTypeFlags.HasFlag(EnricherType.Adult))
        {
            _logger.LogDebug("Clearing adult content fields for photo {PhotoId}", photo.Id);
            photo.IsAdultContent = false;
            photo.AdultScore = 0;
            photo.IsRacyContent = false;
            photo.RacyScore = 0;
        }

        // Clear color fields when Color enricher is being re-run
        if (enricherTypeFlags.HasFlag(EnricherType.Color))
        {
            _logger.LogDebug("Clearing color fields for photo {PhotoId}", photo.Id);
            photo.AccentColor = null;
            photo.DominantColorBackground = null;
            photo.DominantColorForeground = null;
            photo.DominantColors = null;
            photo.IsBW = false;
        }

        // Clear the enricher type flags for the types being re-run
        // The pipeline will set them back after successful enrichment
        photo.EnrichedWithEnricherType &= ~enricherTypeFlags;

        _logger.LogDebug("Updated EnrichedWithEnricherType for photo {PhotoId}: {Flags}",
            photo.Id, photo.EnrichedWithEnricherType);
    }

    /// <summary>
    /// Gets the absolute file path for a photo
    /// </summary>
    private string GetPhotoAbsolutePath(Photo photo)
    {
        if (photo.Files == null || !photo.Files.Any())
        {
            return null;
        }

        var file = photo.Files.First();
        var absolutePath = Path.Combine(photo.Storage.Folder, photo.RelativePath, file.Name);

        if (!File.Exists(absolutePath))
        {
            _logger.LogWarning("Photo file does not exist: {Path}", absolutePath);
            return null;
        }

        return absolutePath;
    }
}
