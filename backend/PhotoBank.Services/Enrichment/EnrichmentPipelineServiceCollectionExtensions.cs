using System;
using System.Collections.Generic;
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

        services.AddSingleton<IEnrichmentPipeline>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EnrichmentPipelineOptions>>();
            var logger = sp.GetRequiredService<ILogger<EnrichmentPipeline>>();
            return new EnrichmentPipeline(sp, enricherTypes, opts, logger);
        });

        return services;
    }
}

