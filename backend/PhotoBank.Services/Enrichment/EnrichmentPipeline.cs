using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichment;

public sealed class EnrichmentPipeline : IEnrichmentPipeline
{
    private readonly IServiceProvider _root;
    private readonly Type[] _enricherTypes;
    private readonly EnrichmentPipelineOptions _opts;
    private readonly ILogger<EnrichmentPipeline> _log;
    private readonly IReadOnlyList<IEnrichmentStopCondition> _globalStopConditions;
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<IEnrichmentStopCondition>> _stopConditionsByEnricher;

    public EnrichmentPipeline(IServiceProvider root,
                              EnricherTypeCatalog enricherCatalog,
                              IOptions<EnrichmentPipelineOptions> opts,
                              IEnumerable<IEnrichmentStopCondition> stopConditions,
                              ILogger<EnrichmentPipeline> log)
    {
        _root = root;
        _enricherTypes = enricherCatalog.Types.ToArray();
        _opts = opts.Value;
        var stopConditionsArray = stopConditions?.ToArray() ?? Array.Empty<IEnrichmentStopCondition>();
        _globalStopConditions = stopConditionsArray
            .Where(c => c.AppliesAfterEnrichers.Count == 0)
            .ToArray();
        _stopConditionsByEnricher = stopConditionsArray
            .SelectMany(
                condition => condition.AppliesAfterEnrichers.DefaultIfEmpty(),
                (condition, enricher) => (condition, enricher))
            .Where(tuple => tuple.enricher is not null)
            .GroupBy(tuple => tuple.enricher!, tuple => tuple.condition)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<IEnrichmentStopCondition>)g.ToArray());
        _log = log;
    }

    public Task<EnrichmentResult> RunAsync(Photo photo, SourceDataDto source, CancellationToken ct = default) =>
        RunAsync(photo, source, _enricherTypes, ct);

    public Task<EnrichmentResult> RunAsync(Photo photo, SourceDataDto source, IReadOnlyCollection<Type> enrichers, CancellationToken ct = default) =>
        RunAsync(photo, source, enrichers, null, ct);

    public async Task<EnrichmentResult> RunAsync(Photo photo, SourceDataDto source, IReadOnlyCollection<Type> enrichers, IServiceProvider? serviceProvider, CancellationToken ct = default)
    {
        var stats = new EnrichmentStats();
        var totalStopwatch = Stopwatch.StartNew();

        // If a service provider is provided, use it directly (for transaction participation).
        // Otherwise, create a new scope to isolate enricher resolution.
        IServiceScope? scope = null;
        IServiceProvider provider;

        if (serviceProvider != null)
        {
            // Use provided service provider - enrichers will share the caller's DbContext
            // and participate in any active transaction
            provider = serviceProvider;
        }
        else
        {
            // Create new scope - enrichers get fresh DbContext instances
            scope = _root.CreateScope();
            provider = scope.ServiceProvider;
        }

        var contextAccessor = provider.GetRequiredService<IEnrichmentContextAccessor>();
        var context = new EnrichmentContext(photo, source);
        var previousContext = contextAccessor.Current;
        contextAccessor.Current = context;

        try
        {
            var ordered = TopoSort(enrichers.ToArray(), provider);
            foreach (var t in ordered)
            {
                ct.ThrowIfCancellationRequested();

                var enricher = (IEnricher)provider.GetRequiredService(t);
                var sw = Stopwatch.StartNew();

                try
                {
                    await enricher.EnrichAsync(photo, source, ct);
                    photo.EnrichedWithEnricherType |= enricher.EnricherType;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Enricher {Enricher} failed", t.Name);
                    if (!_opts.ContinueOnError) throw;
                }
                finally
                {
                    sw.Stop();
                    var elapsed = sw.ElapsedMilliseconds;
                    stats.EnricherTimes[t.Name] = elapsed;

                    if (_opts.LogTimings)
                        _log.LogInformation("Enricher {Enricher} took {Ms} ms", t.Name, elapsed);
                }

                if (EvaluateStopConditions(context, t))
                {
                    break;
                }
            }
        }
        finally
        {
            contextAccessor.Current = previousContext;
            scope?.Dispose();
            totalStopwatch.Stop();
            stats.TotalMilliseconds = totalStopwatch.ElapsedMilliseconds;
        }

        // Return enrichment result with stop reason and statistics
        return new EnrichmentResult(context.StopReason, stats);
    }

    public async Task RunBatchAsync(IEnumerable<(Photo photo, SourceDataDto source)> items, CancellationToken ct = default)
    {
        var arr = items.ToArray();
        var configuredDop = _opts.MaxDegreeOfParallelism;
        var dop = configuredDop.HasValue && configuredDop.Value > 0
            ? configuredDop.Value
            : Math.Max(1, Environment.ProcessorCount);
        _log.LogInformation("Batch enrichment: {Count} items, DOP={Dop}", arr.Length, dop);

        await Parallel.ForEachAsync(arr, new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = ct },
            async (it, token) => await RunAsync(it.photo, it.source, token));
    }

    private static Type[] TopoSort(IReadOnlyList<Type> types, IServiceProvider sp)
    {
        var edges = new Dictionary<Type, HashSet<Type>>();
        foreach (var t in types)
        {
            var deps = new HashSet<Type>(
                ((IOrderDependent)sp.GetRequiredService(t)).Dependencies);

            edges[t] = deps;
        }

        var result = new List<Type>();
        var temp = new HashSet<Type>();
        var perm = new HashSet<Type>();

        void Visit(Type v)
        {
            if (perm.Contains(v)) return;
            if (!temp.Add(v))
                throw new InvalidOperationException($"Dependency cycle detected around {v.Name}");

            foreach (var u in edges[v])
            {
                if (!edges.ContainsKey(u))
                    throw new InvalidOperationException($"Unknown dependency {u.Name} of {v.Name}");
                Visit(u);
            }
            temp.Remove(v);
            perm.Add(v);
            result.Add(v);
        }

        foreach (var t in types) Visit(t);
        return result.ToArray();
    }

    private bool EvaluateStopConditions(EnrichmentContext context, Type enricherType)
    {
        if (EvaluateStopConditions(context, enricherType, _globalStopConditions))
        {
            return true;
        }

        if (_stopConditionsByEnricher.TryGetValue(enricherType, out var targeted) &&
            EvaluateStopConditions(context, enricherType, targeted))
        {
            return true;
        }

        return false;
    }

    private bool EvaluateStopConditions(
        EnrichmentContext context,
        Type enricherType,
        IReadOnlyList<IEnrichmentStopCondition> stopConditions)
    {
        foreach (var stopCondition in stopConditions)
        {
            if (!stopCondition.ShouldStop(context)) continue;

            if (context.TryStop(stopCondition.Reason))
            {
                _log.LogInformation(
                    "Stopping enrichment for photo {PhotoId} after {Enricher}: {Reason}",
                    context.Photo.Id,
                    enricherType.Name,
                    stopCondition.Reason);
            }

            return true;
        }

        return false;
    }
}