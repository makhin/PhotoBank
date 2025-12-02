using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.Services.Enrichment;

/// <summary>
/// Stop condition that checks if DuplicateEnricher found a duplicate photo.
/// If duplicate is detected, stops enrichment to avoid processing the same photo twice.
/// </summary>
public sealed class DuplicateStopCondition : IEnrichmentStopCondition
{
    public IReadOnlyCollection<Type> AppliesAfterEnrichers { get; } = new[] { typeof(DuplicateEnricher) };

    public Task<string?> GetStopReasonAsync(EnrichmentContext context, CancellationToken cancellationToken = default)
    {
        if (context.Source.DuplicatePhotoId.HasValue)
        {
            var reason = $"Duplicate photo detected: {context.Source.DuplicatePhotoInfo ?? $"Photo #{context.Source.DuplicatePhotoId}"}";
            return Task.FromResult<string?>(reason);
        }

        return Task.FromResult<string?>(null);
    }
}
