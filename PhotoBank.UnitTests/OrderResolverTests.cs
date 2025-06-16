using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PhotoBank.DbContext.Models;

namespace PhotoBank.UnitTests
{
    public interface IEnricher
    {
        EnricherType EnricherType { get; }
        EnricherType[] Dependencies { get; }
        Task EnrichAsync();
    }

    public abstract class EnricherTestBase : IEnricher
    {
        public abstract EnricherType EnricherType { get; }
        public abstract EnricherType[] Dependencies { get; }
        public async Task EnrichAsync()
        {
            Debug.WriteLine($"{this.GetType().Name} started at {DateTime.Now}");
            Console.WriteLine($"{this.GetType().Name} started at {DateTime.Now}");

            Random random = new Random();
            int delay = random.Next(10000, 15000);
            await Task.Delay(delay);

            Debug.WriteLine($"{this.GetType().Name} finished at {DateTime.Now} delay {delay}");
            Console.WriteLine($"{this.GetType().Name} finished at {DateTime.Now} delay {delay}");
        }
    }

    public class EnAd : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Adult;

        public override EnricherType[] Dependencies => new[] { EnricherType.Analyze };
    }

    public class EnAn : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Analyze;
        public override EnricherType[] Dependencies => new[] { EnricherType.Preview };
    }

    public class EnCap : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Caption;
        public override EnricherType[] Dependencies => new[] { EnricherType.Analyze };
    }

    public class EnCat : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Category;
        public override EnricherType[] Dependencies => new[] { EnricherType.Analyze };
    }

    public class EnCo : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Color;
        public override EnricherType[] Dependencies => new[] { EnricherType.Analyze };
    }

    public class EnFa : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Face;
        public override EnricherType[] Dependencies => new[] { EnricherType.Analyze };
    }

    public class EnMe : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Metadata;
        public override EnricherType[] Dependencies => new[] { EnricherType.Preview };
    }

    public class EnOb : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.ObjectProperty;
        public override EnricherType[] Dependencies => new[] { EnricherType.Analyze };
    }

    public class EnPr : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Preview;
        public override EnricherType[] Dependencies => Array.Empty<EnricherType>();
    }

    public class EnTa : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Tag;
        public override EnricherType[] Dependencies => new[] { EnricherType.Analyze };
    }

    public class EnTh : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Thumbnail;
        public override EnricherType[] Dependencies => new[] { EnricherType.Preview };
    }

    public class DependencyExecutor
    {
        public async Task ExecuteAsync(IEnumerable<EnricherTestBase> enrichers)
        {
            var enricherList = enrichers.ToList();
            var typeToInstance = enricherList.ToDictionary(e => e.EnricherType);
            var dependencyGraph = new Dictionary<EnricherType, List<EnricherType>>();
            var incomingEdges = new Dictionary<EnricherType, int>();

            // Инициализация графа
            foreach (var enricher in enricherList)
            {
                var type = enricher.EnricherType;
                incomingEdges[type] = 0;
                dependencyGraph[type] = new List<EnricherType>();
            }

            // Построение зависимостей
            foreach (var enricher in enricherList)
            {
                var currentType = enricher.EnricherType;
                foreach (var dependencyType in enricher.Dependencies)
                {
                    if (!dependencyGraph.ContainsKey(dependencyType))
                        throw new InvalidOperationException($"Missing dependency: {dependencyType}");

                    dependencyGraph[dependencyType].Add(currentType);
                    incomingEdges[currentType]++;
                }
            }

            var readyQueue = new Queue<EnricherType>(
                incomingEdges.Where(kv => kv.Value == 0).Select(kv => kv.Key)
            );

            var completed = new HashSet<EnricherType>();
            var runningTasks = new Dictionary<EnricherType, Task>();

            while (readyQueue.Count > 0 || runningTasks.Count > 0)
            {
                while (readyQueue.Count > 0)
                {
                    var type = readyQueue.Dequeue();
                    var enricher = typeToInstance[type];

                    var task = Task.Run(async () =>
                    {
                        await enricher.EnrichAsync();
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

            Debug.WriteLine($"Done {DateTime.Now}");
            Console.WriteLine($"Done {DateTime.Now}");
        }
    }

    [TestFixture]
    public class OrderResolverTests
    {
        [Test]
        public async Task Test()
        {
            IEnumerable<EnricherTestBase> enrichers = new EnricherTestBase[]{ new EnAd(), new EnAn(), new EnCap(), new EnCat(), new EnCo(), new EnFa(), new EnMe(), new EnOb(), new EnPr(), new EnTa(), new EnTh() };
            var executor = new DependencyExecutor();
            await executor.ExecuteAsync(enrichers);
        }
    }
}
