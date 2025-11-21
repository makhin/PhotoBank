using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.Services.Enrichment;

/// <summary>
/// Calculates which enrichers need to be run based on already applied enrichers and active enricher configuration
/// </summary>
public class EnricherDiffCalculator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Enricher types that populate SourceDataDto fields (not persisted to database).
    /// These must be re-run even if already applied, since their data is needed by dependent enrichers.
    /// </summary>
    private static readonly HashSet<EnricherType> DataProviderEnricherTypes = new()
    {
        EnricherType.Preview,  // Populates PreviewImage, OriginalImage in SourceDataDto
        EnricherType.Analyze   // Populates ImageAnalysis in SourceDataDto
    };

    public EnricherDiffCalculator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Calculates which enrichers need to be run for a photo
    /// </summary>
    /// <param name="photo">The photo to check</param>
    /// <param name="activeEnricherTypes">Currently active enricher types from configuration</param>
    /// <returns>List of enricher types that need to be executed</returns>
    public virtual IReadOnlyCollection<Type> CalculateMissingEnrichers(Photo photo, IReadOnlyCollection<Type> activeEnricherTypes)
    {
        if (photo == null)
            throw new ArgumentNullException(nameof(photo));

        if (activeEnricherTypes == null || !activeEnricherTypes.Any())
            return Array.Empty<Type>();

        var alreadyApplied = photo.EnrichedWithEnricherType;
        var enrichersToRun = new HashSet<Type>();
        var visitingStack = new HashSet<Type>();

        // For each active enricher, check if it needs to be run
        foreach (var enricherType in activeEnricherTypes)
        {
            AddEnricherWithDependencies(enricherType, alreadyApplied, enrichersToRun, visitingStack);
        }

        return enrichersToRun.ToArray();
    }

    /// <summary>
    /// Recursively adds an enricher and its dependencies if they haven't been applied yet
    /// </summary>
    /// <param name="enricherType">The enricher type to add</param>
    /// <param name="alreadyApplied">Bitmask of already applied enrichers</param>
    /// <param name="enrichersToRun">Set of enrichers that need to be run</param>
    /// <param name="visitingStack">Set of types currently being visited (for cycle detection)</param>
    private void AddEnricherWithDependencies(
        Type enricherType,
        EnricherType alreadyApplied,
        HashSet<Type> enrichersToRun,
        HashSet<Type> visitingStack)
    {
        // Get enricher instance to check its type and dependencies
        var enricher = (IEnricher)_serviceProvider.GetRequiredService(enricherType);

        // Check if this enricher has already been applied
        // Exception: Data provider enrichers must run even if already applied,
        // because they populate SourceDataDto fields that aren't persisted to DB
        var isDataProvider = DataProviderEnricherTypes.Contains(enricher.EnricherType);
        if (!isDataProvider && (alreadyApplied & enricher.EnricherType) != 0)
        {
            // Already applied and not a data provider, skip
            return;
        }

        // Check if already in the result set (dependencies already processed)
        if (enrichersToRun.Contains(enricherType))
        {
            return;
        }

        // Check for cyclic dependency
        if (visitingStack.Contains(enricherType))
        {
            throw new InvalidOperationException($"Dependency cycle detected around {enricherType.Name}");
        }

        // Mark as visiting
        visitingStack.Add(enricherType);

        try
        {
            // Check and add dependencies first
            if (enricher.Dependencies != null)
            {
                foreach (var dependency in enricher.Dependencies)
                {
                    AddEnricherWithDependencies(dependency, alreadyApplied, enrichersToRun, visitingStack);
                }
            }

            // Add this enricher to the list
            enrichersToRun.Add(enricherType);
        }
        finally
        {
            // Remove from visiting stack
            visitingStack.Remove(enricherType);
        }
    }

    /// <summary>
    /// Checks if a photo needs any enrichment based on active enrichers
    /// </summary>
    /// <param name="photo">The photo to check</param>
    /// <param name="activeEnricherTypes">Currently active enricher types</param>
    /// <returns>True if the photo needs enrichment, false otherwise</returns>
    public virtual bool NeedsEnrichment(Photo photo, IReadOnlyCollection<Type> activeEnricherTypes)
    {
        var missing = CalculateMissingEnrichers(photo, activeEnricherTypes);
        return missing.Any();
    }

    /// <summary>
    /// Expands a list of enricher types to include all their dependencies.
    /// This is necessary for EnrichmentPipeline which requires all dependencies to be present in the list
    /// for topological sorting, even if some dependencies are already applied.
    /// </summary>
    /// <param name="enrichers">Enricher types to expand</param>
    /// <returns>Enricher types with all dependencies included</returns>
    public virtual IReadOnlyCollection<Type> ExpandWithDependencies(IReadOnlyCollection<Type> enrichers)
    {
        if (enrichers == null || !enrichers.Any())
            return Array.Empty<Type>();

        var result = new HashSet<Type>();
        var visitingStack = new HashSet<Type>();

        foreach (var enricherType in enrichers)
        {
            AddEnricherWithAllDependencies(enricherType, result, visitingStack);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Recursively adds an enricher and ALL its dependencies to the result set,
    /// regardless of whether they've already been applied to a photo.
    /// This ensures EnrichmentPipeline has all dependencies needed for topological sorting.
    /// </summary>
    private void AddEnricherWithAllDependencies(
        Type enricherType,
        HashSet<Type> result,
        HashSet<Type> visitingStack)
    {
        // Already processed
        if (result.Contains(enricherType))
            return;

        // Check for cyclic dependency
        if (visitingStack.Contains(enricherType))
            throw new InvalidOperationException($"Dependency cycle detected around {enricherType.Name}");

        visitingStack.Add(enricherType);

        try
        {
            var enricher = (IEnricher)_serviceProvider.GetRequiredService(enricherType);

            // Recursively add all dependencies first
            if (enricher.Dependencies != null)
            {
                foreach (var dependency in enricher.Dependencies)
                {
                    AddEnricherWithAllDependencies(dependency, result, visitingStack);
                }
            }

            // Add this enricher to the result
            result.Add(enricherType);
        }
        finally
        {
            visitingStack.Remove(enricherType);
        }
    }

    /// <summary>
    /// Gets enricher types that have been applied to a photo
    /// </summary>
    /// <param name="photo">The photo to check</param>
    /// <param name="allEnricherTypes">All available enricher types</param>
    /// <returns>List of enricher types that have been applied</returns>
    public virtual IReadOnlyCollection<Type> GetAppliedEnrichers(Photo photo, IReadOnlyCollection<Type> allEnricherTypes)
    {
        if (photo == null)
            throw new ArgumentNullException(nameof(photo));

        var applied = new List<Type>();
        var alreadyApplied = photo.EnrichedWithEnricherType;

        foreach (var enricherType in allEnricherTypes)
        {
            var enricher = (IEnricher)_serviceProvider.GetRequiredService(enricherType);
            if ((alreadyApplied & enricher.EnricherType) != 0)
            {
                applied.Add(enricherType);
            }
        }

        return applied;
    }
}
