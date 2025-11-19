using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.Services.Enrichment;

public static class EnrichmentPipelineServiceCollectionExtensions
{
    /// <summary>
    /// Registers all <see cref="IEnricher"/> implementations found in the provided assemblies and the pipeline itself.
    /// </summary>
    public static IServiceCollection AddEnrichmentPipeline(
        this IServiceCollection services,
        Action<EnrichmentPipelineOptions>? configure,
        params Assembly[] scanAssemblies)
    {
        if (scanAssemblies == null || scanAssemblies.Length == 0)
            scanAssemblies = new[] { Assembly.GetExecutingAssembly() };

        var enricherTypes = new HashSet<Type>();
        foreach (var asm in scanAssemblies)
        {
            foreach (var t in asm.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface) continue;
                if (typeof(IEnricher).IsAssignableFrom(t))
                {
                    services.AddTransient(t);
                    enricherTypes.Add(t);
                }
            }
        }

        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<EnrichmentPipelineOptions>(_ => { });

        services.AddSingleton(_ => new EnricherTypeCatalog(enricherTypes));
        services.AddSingleton<IEnrichmentPipeline, EnrichmentPipeline>();

        return services;
    }

    /// <summary>
    /// Registers enrichment infrastructure (pipeline, catalog, re-enrichment service) using already registered enrichers.
    /// Call this AFTER registering enricher implementations with services.AddTransient&lt;IEnricher, YourEnricher&gt;().
    /// </summary>
    public static IServiceCollection AddEnrichmentInfrastructure(
        this IServiceCollection services,
        Action<EnrichmentPipelineOptions>? configure = null)
    {
        // Collect already registered enricher types from service descriptors
        var enricherTypes = services
            .Where(d => d.ServiceType == typeof(IEnricher) && d.ImplementationType is not null)
            .Select(d => d.ImplementationType!)
            .Distinct()
            .ToArray();

        if (enricherTypes.Length == 0)
        {
            throw new InvalidOperationException(
                "No enrichers registered. Register enricher implementations before calling AddEnrichmentInfrastructure(). " +
                "Example: services.AddTransient<IEnricher, MetadataEnricher>();");
        }

        // Register each concrete enricher type so EnrichmentPipeline and EnricherDiffCalculator
        // can resolve them by concrete type via provider.GetRequiredService(enricherType)
        foreach (var enricherType in enricherTypes)
        {
            services.AddTransient(enricherType);
        }

        // Configure pipeline options
        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<EnrichmentPipelineOptions>(_ => { });

        // Register enrichment infrastructure
        services.AddSingleton(_ => new EnricherTypeCatalog(enricherTypes));
        services.AddSingleton<IEnrichmentPipeline, EnrichmentPipeline>();
        services.AddScoped<EnricherDiffCalculator>();
        services.AddScoped<IReEnrichmentService, ReEnrichmentService>();

        return services;
    }
}


