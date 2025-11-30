using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services.Enrichment;

public interface IEnrichmentStopCondition
{
    /// <summary>
    /// Enrichers after which this condition should be evaluated. Empty collection means evaluate after every enricher.
    /// </summary>
    IReadOnlyCollection<Type> AppliesAfterEnrichers { get; }

    Task<string?> GetStopReasonAsync(EnrichmentContext context, CancellationToken cancellationToken = default);
}

public sealed class PredicateEnrichmentStopCondition : IEnrichmentStopCondition
{
    private readonly Func<EnrichmentContext, Task<string?>> _predicate;
    private readonly IReadOnlyCollection<Type> _appliesAfter;

    public PredicateEnrichmentStopCondition(
        Func<EnrichmentContext, Task<string?>> predicate,
        IEnumerable<Type>? appliesAfterEnrichers = null)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _appliesAfter = appliesAfterEnrichers?.ToArray() ?? Array.Empty<Type>();
    }

    public IReadOnlyCollection<Type> AppliesAfterEnrichers => _appliesAfter;

    public Task<string?> GetStopReasonAsync(EnrichmentContext context, CancellationToken cancellationToken = default) =>
        _predicate(context);
}
