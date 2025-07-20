using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.Services;

public interface IDependencyExecutor
{
    Task ExecuteAsync(IEnumerable<IEnricher> enrichers, Photo photo, SourceDataDto sourceData);
}

public class DependencyExecutor : IDependencyExecutor
{
    private readonly int _concurrencyLimit;

    public DependencyExecutor(int concurrencyLimit = 4)
    {
        _concurrencyLimit = concurrencyLimit;
    }

    public async Task ExecuteAsync(IEnumerable<IEnricher> enrichers, Photo photo, SourceDataDto sourceData)
    {
        var enricherList = enrichers.ToList();
        var typeToInstance = enricherList.ToDictionary(e => e.GetType());
        var dependencyGraph = new Dictionary<Type, List<Type>>();
        var incomingEdges = new ConcurrentDictionary<Type, int>();

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
                incomingEdges.AddOrUpdate(currentType, 1, (_, v) => v + 1);
            }
        }

        var readyQueue = new ConcurrentQueue<Type>(
            incomingEdges.Where(kv => kv.Value == 0).Select(kv => kv.Key)
        );

        var runningTasks = new Dictionary<Type, Task>();
        var semaphore = new SemaphoreSlim(_concurrencyLimit);
        var completedCount = 0;

        while (!readyQueue.IsEmpty || runningTasks.Count > 0)
        {
            while (runningTasks.Count < _concurrencyLimit && readyQueue.TryDequeue(out var type))
            {
                var enricher = typeToInstance[type];
                await semaphore.WaitAsync();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await enricher.EnrichAsync(photo, sourceData);
                        if (photo != null)
                            photo.EnrichedWithEnricherType |= enricher.EnricherType;

                        foreach (var dependent in dependencyGraph[type])
                        {
                            var newCount = incomingEdges.AddOrUpdate(dependent, 0, (_, v) => v - 1);
                            if (newCount == 0)
                                readyQueue.Enqueue(dependent);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                runningTasks[type] = task;
            }

            if (runningTasks.Count > 0)
            {
                var finished = await Task.WhenAny(runningTasks.Values);
                var finishedEntry = runningTasks.First(kvp => kvp.Value == finished);
                runningTasks.Remove(finishedEntry.Key);
                await finished;
                completedCount++;
            }
            else if (readyQueue.IsEmpty)
            {
                break;
            }
        }

        if (completedCount < enricherList.Count)
            throw new InvalidOperationException("Cycle detected in dependencies");
    }
}