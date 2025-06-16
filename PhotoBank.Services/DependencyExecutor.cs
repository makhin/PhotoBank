using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoBank.Services;

public interface IDependencyExecutor
{
    Task ExecuteAsync(IEnumerable<IEnricher> enrichers, Photo photo, SourceDataDto sourceData);
}

public class DependencyExecutor : IDependencyExecutor
{
    public async Task ExecuteAsync(IEnumerable<IEnricher> enrichers, Photo photo, SourceDataDto sourceData)
    {
        var enricherList = enrichers.ToList();
        var typeToInstance = enricherList.ToDictionary(e => e.GetType());
        var dependencyGraph = new Dictionary<Type, List<Type>>();
        var incomingEdges = new Dictionary<Type, int>();

        // Инициализация графа
        foreach (var enricher in enricherList)
        {
            var type = enricher.GetType();
            incomingEdges[type] = 0;
            dependencyGraph[type] = new List<Type>();
        }

        // Построение зависимостей
        foreach (var enricher in enricherList)
        {
            var currentType = enricher.GetType();
            foreach (var dependencyType in enricher.Dependencies)
            {
                if (!dependencyGraph.ContainsKey(dependencyType))
                    throw new InvalidOperationException($"Missing dependency: {dependencyType}");

                dependencyGraph[dependencyType].Add(currentType);
                incomingEdges[currentType]++;
            }
        }

        var readyQueue = new Queue<Type>(
            incomingEdges.Where(kv => kv.Value == 0).Select(kv => kv.Key)
        );

        var completed = new HashSet<Type>();
        var runningTasks = new Dictionary<Type, Task>();

        while (readyQueue.Count > 0 || runningTasks.Count > 0)
        {
            while (readyQueue.Count > 0)
            {
                var type = readyQueue.Dequeue();
                var enricher = typeToInstance[type];

                var task = Task.Run(async () =>
                {
                    await enricher.EnrichAsync(photo, sourceData);
                    if (photo != null)
                        photo.EnrichedWithEnricherType |= enricher.EnricherType;
                    lock (completed)
                    {
                        completed.Add(type);
                        foreach (var dependent in dependencyGraph[type])
                        {
                            incomingEdges[dependent]--;
                            if (incomingEdges[dependent] == 0)
                            {
                                readyQueue.Enqueue(dependent);
                            }
                        }
                    }
                });

                runningTasks[type] = task;
            }

            var finished = await Task.WhenAny(runningTasks.Values);
            var finishedEntry = runningTasks.First(kvp => kvp.Value == finished);
            runningTasks.Remove(finishedEntry.Key);
        }

        if (completed.Count < enricherList.Count)
            throw new InvalidOperationException("Cycle detected in dependencies");
    }
}