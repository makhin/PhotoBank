using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests
{
    public abstract class EnricherTestBase : IEnricher
    {
        public abstract EnricherType EnricherType { get; }
        public abstract Type[] Dependencies { get; }
        public async Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default)
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
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnAn : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Analyze;
        public override Type[] Dependencies => new[] { typeof(EnPr) };
    }

    public class EnCap : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Caption;
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnCat : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Category;
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnCo : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Color;
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnFa : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Face;
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnMe : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Metadata;
        public override Type[] Dependencies => new[] { typeof(EnPr) };
    }

    public class EnOb : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.ObjectProperty;
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnPr : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Preview;
        public override Type[] Dependencies => Array.Empty<Type>();
    }

    public class EnTa : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Tag;
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnTh : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Thumbnail;
        public override Type[] Dependencies => new[] { typeof(EnPr) };
    }

    // Helper for circular dependency
    public class CircularEnricher1 : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Adult;
        public override Type[] Dependencies => new[] { typeof(CircularEnricher2) };
    }

    public class CircularEnricher2 : EnricherTestBase
    {
        public override EnricherType EnricherType => EnricherType.Adult;
        public override Type[] Dependencies => new[] { typeof(CircularEnricher1) };
    }

    [TestFixture]
    public class DependencyExecutorTests
    {
        [Test]
        public async Task ExecuteAsync_AllDependenciesResolved_CompletesSuccessfully()
        {
            // Arrange
            IEnumerable<EnricherTestBase> enrichers = new EnricherTestBase[]
            {
                new EnAd(),
                new EnAn(),
                new EnCap(),
                new EnCat(),
                new EnCo(),
                new EnFa(),
                new EnMe(),
                new EnOb(),
                new EnPr(),
                new EnTa(),
                new EnTh()
            };
            var executor = new DependencyExecutor();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await executor.ExecuteAsync(enrichers, null, null));
        }

        [Test]
        public void ExecuteAsync_MissingDependency_ThrowsInvalidOperationException()
        {
            // Arrange: Remove EnPr, which is a dependency for others
            IEnumerable<EnricherTestBase> enrichers = new EnricherTestBase[]
            {
                new EnAd(), new EnAn(), new EnCap(), new EnCat(), new EnCo(),
                new EnFa(), new EnMe(), new EnOb(), /* new EnPr(), */ new EnTa(), new EnTh()
            };
            var executor = new DependencyExecutor();

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.ExecuteAsync(enrichers, null, null));
        }

        [Test]
        public void ExecuteAsync_CircularDependency_ThrowsInvalidOperationException()
        {
            // Arrange: Create a circular dependency
            var circular1 = new CircularEnricher1();
            var circular2 = new CircularEnricher2();
            var pr = new EnPr();
            IEnumerable<EnricherTestBase> enrichers = new EnricherTestBase[] { circular1, circular2 };
            var executor = new DependencyExecutor();

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.ExecuteAsync(enrichers, null, null));
        }
    }
}
