using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.Services.Enrichment;

public sealed class ActiveEnricherProvider : IActiveEnricherProvider
{
    private static readonly Lazy<IReadOnlyDictionary<string, Type>> EnricherTypes =
        new(BuildEnricherTypeMap, LazyThreadSafetyMode.ExecutionAndPublication);

    public IReadOnlyCollection<Type> GetActiveEnricherTypes(IRepository<Enricher> repository)
    {
        if (repository is null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        var activeNames = repository.GetAll()
            .Where(e => e.IsActive)
            .Select(e => e.Name)
            .ToArray();

        var result = new List<Type>(activeNames.Length);
        foreach (var name in activeNames)
        {
            if (!EnricherTypes.Value.TryGetValue(name, out var type))
            {
                throw new NotSupportedException($"Enricher '{name}' not found in loaded assemblies.");
            }

            result.Add(type);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, Type> BuildEnricherTypeMap()
    {
        return typeof(IEnricher).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(IEnricher).IsAssignableFrom(t))
            .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
    }
}
