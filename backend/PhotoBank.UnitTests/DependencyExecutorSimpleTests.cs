using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests
{
    public abstract class TestEnricherBaseSimple : IEnricher
    {
        private readonly IList<string> _log;
        protected TestEnricherBaseSimple(IList<string> log)
        {
            _log = log;
        }

        public abstract EnricherType EnricherType { get; }
        public abstract Type[] Dependencies { get; }

        public Task EnrichAsync(Photo photo, SourceDataDto sourceData)
        {
            _log.Add(GetType().Name);
            return Task.CompletedTask;
        }
    }

    public class AEnricher : TestEnricherBaseSimple
    {
        public AEnricher(IList<string> log) : base(log) { }
        public override EnricherType EnricherType => EnricherType.Adult;
        public override Type[] Dependencies => Array.Empty<Type>();
    }

    public class BEnricher : TestEnricherBaseSimple
    {
        public BEnricher(IList<string> log) : base(log) { }
        public override EnricherType EnricherType => EnricherType.Metadata;
        public override Type[] Dependencies => new[] { typeof(AEnricher) };
    }

    public class CEnricher : TestEnricherBaseSimple
    {
        public CEnricher(IList<string> log) : base(log) { }
        public override EnricherType EnricherType => EnricherType.Tag;
        public override Type[] Dependencies => new[] { typeof(BEnricher) };
    }

    public class BlockingEnricher : IEnricher
    {
        private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release;

        public BlockingEnricher(EnricherType type, TaskCompletionSource<bool> release)
        {
            EnricherType = type;
            _release = release;
        }

        public EnricherType EnricherType { get; }
        public Type[] Dependencies => Array.Empty<Type>();
        public Task Started => _started.Task;

        public async Task EnrichAsync(Photo photo, SourceDataDto sourceData)
        {
            _started.SetResult(true);
            await _release.Task;
        }
    }

    public class BlockingEnricher1 : BlockingEnricher
    {
        public BlockingEnricher1(TaskCompletionSource<bool> release) : base(EnricherType.Adult, release) { }
    }

    public class BlockingEnricher2 : BlockingEnricher
    {
        public BlockingEnricher2(TaskCompletionSource<bool> release) : base(EnricherType.Metadata, release) { }
    }

    [TestFixture]
    public class DependencyExecutorSimpleTests
    {
        [Test]
        public async Task ExecuteAsync_UpdatesPhotoEnrichedTypes()
        {
            var log = new List<string>();
            var enrichers = new IEnricher[]
            {
                new AEnricher(log),
                new BEnricher(log),
                new CEnricher(log)
            };
            var executor = new DependencyExecutor();
            var photo = new Photo();

            await executor.ExecuteAsync(enrichers, photo, null);

            photo.EnrichedWithEnricherType.Should().Be(
                EnricherType.Adult | EnricherType.Metadata | EnricherType.Tag);
            log.Should().HaveCount(3);
        }

        [Test]
        public async Task ExecuteAsync_RespectsDependencyOrder()
        {
            var log = new List<string>();
            var enrichers = new IEnricher[]
            {
                new AEnricher(log),
                new BEnricher(log),
                new CEnricher(log)
            };
            var executor = new DependencyExecutor();

            await executor.ExecuteAsync(enrichers, new Photo(), null);

            log.Should().Equal(
                nameof(AEnricher),
                nameof(BEnricher),
                nameof(CEnricher));
        }

        [Test]
        public async Task ExecuteAsync_RunsIndependentEnrichersInParallel()
        {
            var release1 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release2 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var enricher1 = new BlockingEnricher1( release1);
            var enricher2 = new BlockingEnricher2(release2);
            var executor = new DependencyExecutor();

            var executeTask = executor.ExecuteAsync(new IEnricher[] { enricher1, enricher2 }, new Photo(), null);

            var bothStarted = Task.WhenAll(enricher1.Started, enricher2.Started);
            var finished = await Task.WhenAny(bothStarted, Task.Delay(1000));
            
            release1.SetResult(true);
            release2.SetResult(true);

            await executeTask;

            Assert.That(finished, Is.EqualTo(bothStarted), "Enrichers did not start in parallel");
        }
    }
}
