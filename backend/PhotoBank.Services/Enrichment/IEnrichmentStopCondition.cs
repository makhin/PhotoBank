using System;

namespace PhotoBank.Services.Enrichment;

public interface IEnrichmentStopCondition
{
    string Reason { get; }

    bool ShouldStop(EnrichmentContext context);
}

public sealed class PredicateEnrichmentStopCondition : IEnrichmentStopCondition
{
    private readonly Func<EnrichmentContext, bool> _predicate;

    public PredicateEnrichmentStopCondition(string reason, Func<EnrichmentContext, bool> predicate)
    {
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    public string Reason { get; }

    public bool ShouldStop(EnrichmentContext context) => _predicate(context);
}
