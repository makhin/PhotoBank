using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.UnitTests
{
    public abstract class EnricherTestBase : IEnricher
    {
        public EnricherType EnricherType => EnricherType.None;
        public abstract Type[] Dependencies { get; }
        public async Task EnrichAsync(Photo photo, SourceDataDto path)
        {
            await Task.Run(() =>
            {
                Task.Delay(new Random().Next(5000, 15000));
                Debug.WriteLine($"{this.GetType().Name} started at {DateTime.Now}");
                Console.WriteLine($"{this.GetType().Name} started at {DateTime.Now}");
            });
        }
    }

    public class EnAd : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnAn : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnPr) };
    }

    public class EnCap : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnCat : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnCo : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnFa : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnMe : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnPr) };
    }

    public class EnOb : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnPr : EnricherTestBase
    {
        public override Type[] Dependencies => Array.Empty<Type>();
    }

    public class EnTa : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnTh : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnPr) };
    }

    [TestFixture]
    public class OrderResolverTests
    {
        [Test]
        public async Task Test()
        {
            IEnumerable<IEnricher> enrichers = new EnricherTestBase[]{ new EnAd(), new EnAn(), new EnCap(), new EnCat(), new EnCo(), new EnFa(), new EnMe(), new EnOb(), new EnPr(), new EnTa(), new EnTh() };
            var orderedEnrichers = ResolveOrder(enrichers);
            await RunEnrichersAsync(orderedEnrichers);
        }

        private static List<IEnricher> ResolveOrder(IEnumerable<IEnricher> enrichers)
        {
            var resolved = new List<IEnricher>();
            var unresolved = new HashSet<IEnricher>(enrichers);

            while (unresolved.Any())
            {
                bool progress = false;

                foreach (var enricher in unresolved.ToList())
                {
                    if (enricher.Dependencies.All(d => resolved.Any(r => r.GetType() == d)))
                    {
                        resolved.Add(enricher);
                        unresolved.Remove(enricher);
                        progress = true;
                    }
                }

                if (!progress)
                {
                    throw new InvalidOperationException("Circular dependency detected or missing dependency.");
                }
            }

            return resolved;
        }

        private static async Task RunEnrichersAsync(IEnumerable<IEnricher> enrichers)
        {
            var photo = new Photo();
            var path = new SourceDataDto();
            var tasks = new Dictionary<Type, TaskCompletionSource<bool>>();

            foreach (var enricher in enrichers)
            {
                tasks[enricher.GetType()] = new TaskCompletionSource<bool>();
            }

            foreach (var enricher in enrichers)
            {
                var dependencyTasks = enricher.Dependencies.Select(d => tasks[d].Task).ToArray();
                var task = Task.WhenAll(dependencyTasks).ContinueWith(async _ =>
                {
                    await enricher.EnrichAsync(photo, path);
                    tasks[enricher.GetType()].SetResult(true);
                }).Unwrap();
            }

            await Task.WhenAll(tasks.Values.Select(t => t.Task));
        }
    }
}
