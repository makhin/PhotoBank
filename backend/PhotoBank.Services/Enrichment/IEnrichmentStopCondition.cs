using System;
using System.Collections.Generic;
using System.Linq;

namespace PhotoBank.Services.Enrichment;

public interface IEnrichmentStopCondition
{
    string Reason { get; }

    /// <summary>
    /// Enrichers after which this condition should be evaluated. Empty collection means evaluate after every enricher.
    /// </summary>
    IReadOnlyCollection<Type> AppliesAfterEnrichers { get; }

    bool ShouldStop(EnrichmentContext context);
}

public sealed class PredicateEnrichmentStopCondition : IEnrichmentStopCondition
{
    private readonly Func<EnrichmentContext, bool> _predicate;
    private readonly IReadOnlyCollection<Type> _appliesAfter;

    public PredicateEnrichmentStopCondition(
        string reason,
        Func<EnrichmentContext, bool> predicate,
        IEnumerable<Type>? appliesAfterEnrichers = null)
    {
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _appliesAfter = appliesAfterEnrichers?.ToArray() ?? Array.Empty<Type>();
    }

    public string Reason { get; }

    public IReadOnlyCollection<Type> AppliesAfterEnrichers => _appliesAfter;

    public bool ShouldStop(EnrichmentContext context) => _predicate(context);
}
