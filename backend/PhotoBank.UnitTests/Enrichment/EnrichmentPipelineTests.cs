using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichment;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichment;

[TestFixture]
public class EnrichmentPipelineTests
{
    private static (EnrichmentPipeline Pipeline, ServiceProvider Provider) CreatePipeline(IList<string> log)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IList<string>>(log);
        services.AddScoped<AlphaEnricher>();
        services.AddScoped<BravoEnricher>();
        services.AddScoped<CharlieEnricher>();

        var provider = services.BuildServiceProvider();
        var catalog = new EnricherTypeCatalog(new[] { typeof(AlphaEnricher), typeof(BravoEnricher), typeof(CharlieEnricher) });
        var pipeline = new EnrichmentPipeline(
            provider,
            catalog,
            Options.Create(new EnrichmentPipelineOptions { LogTimings = false }),
            NullLogger<EnrichmentPipeline>.Instance);

        return (pipeline, provider);
    }

    [Test]
    public async Task RunAsync_SubsetRespectsDependencyOrder()
    {
        var log = new List<string>();
        var (pipeline, provider) = CreatePipeline(log);

        try
        {
            await pipeline.RunAsync(
                new Photo(),
                new SourceDataDto(),
                new[] { typeof(CharlieEnricher), typeof(AlphaEnricher), typeof(BravoEnricher) },
                CancellationToken.None);
        }
        finally
        {
            provider.Dispose();
        }

        log.Should().Equal(
            nameof(AlphaEnricher),
            nameof(BravoEnricher),
            nameof(CharlieEnricher));
    }

    [Test]
    public async Task RunAsync_SubsetUpdatesEnrichedWithFlags()
    {
        var log = new List<string>();
        var (pipeline, provider) = CreatePipeline(log);

        var photo = new Photo();

        try
        {
            await pipeline.RunAsync(
                photo,
                new SourceDataDto(),
                new[] { typeof(AlphaEnricher), typeof(BravoEnricher), typeof(CharlieEnricher) },
                CancellationToken.None);
        }
        finally
        {
            provider.Dispose();
        }

        photo.EnrichedWithEnricherType.Should().Be(
            EnricherType.Analyze | EnricherType.Metadata | EnricherType.Tag);
    }

    [Test]
    public async Task RunAsync_SubsetMissingDependency_Throws()
    {
        var log = new List<string>();
        var (pipeline, provider) = CreatePipeline(log);

        try
        {
            var act = () => pipeline.RunAsync(
                new Photo(),
                new SourceDataDto(),
                new[] { typeof(CharlieEnricher) },
                CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Unknown dependency*");
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Test]
    public async Task RunAsync_CircularDependency_Throws()
    {
        var services = new ServiceCollection();
        services.AddScoped<CycleAlphaEnricher>();
        services.AddScoped<CycleBravoEnricher>();

        var provider = services.BuildServiceProvider();
        var catalog = new EnricherTypeCatalog(new[] { typeof(CycleAlphaEnricher), typeof(CycleBravoEnricher) });
        var pipeline = new EnrichmentPipeline(
            provider,
            catalog,
            Options.Create(new EnrichmentPipelineOptions { LogTimings = false }),
            NullLogger<EnrichmentPipeline>.Instance);

        try
        {
            var act = () => pipeline.RunAsync(
                new Photo(),
                new SourceDataDto(),
                new[] { typeof(CycleAlphaEnricher), typeof(CycleBravoEnricher) },
                CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*cycle detected*");
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Test]
    public async Task RunBatchAsync_ProcessesItemsInParallel()
    {
        var state = new BlockingEnricherState(expectedStarts: 2);
        var services = new ServiceCollection();
        services.AddSingleton(state);
        services.AddScoped<BlockingEnricher>();

        var provider = services.BuildServiceProvider();
        var catalog = new EnricherTypeCatalog(new[] { typeof(BlockingEnricher) });
        var pipeline = new EnrichmentPipeline(
            provider,
            catalog,
            Options.Create(new EnrichmentPipelineOptions
            {
                LogTimings = false,
                MaxDegreeOfParallelism = 2
            }),
            NullLogger<EnrichmentPipeline>.Instance);

        try
        {
            var work = new[]
            {
                (new Photo(), new SourceDataDto()),
                (new Photo(), new SourceDataDto())
            };

            var runTask = pipeline.RunBatchAsync(work, CancellationToken.None);
            var completed = await Task.WhenAny(state.BothStarted, Task.Delay(1000));

            completed.Should().BeSameAs(state.BothStarted);

            state.ReleaseAll();
            await runTask;
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Test]
    public async Task RunBatchAsync_MaxParallelismLessOrEqualZero_FallsBackToProcessorCount()
    {
        Assume.That(Environment.ProcessorCount, Is.GreaterThanOrEqualTo(2),
            "Test requires at least two processors to observe parallelism.");

        var state = new BlockingEnricherState(expectedStarts: 2);
        var services = new ServiceCollection();
        services.AddSingleton(state);
        services.AddScoped<BlockingEnricher>();

        var provider = services.BuildServiceProvider();
        var catalog = new EnricherTypeCatalog(new[] { typeof(BlockingEnricher) });
        var pipeline = new EnrichmentPipeline(
            provider,
            catalog,
            Options.Create(new EnrichmentPipelineOptions
            {
                LogTimings = false,
                MaxDegreeOfParallelism = 0
            }),
            NullLogger<EnrichmentPipeline>.Instance);

        try
        {
            var work = new[]
            {
                (new Photo(), new SourceDataDto()),
                (new Photo(), new SourceDataDto())
            };

            var runTask = pipeline.RunBatchAsync(work, CancellationToken.None);
            var completed = await Task.WhenAny(state.BothStarted, Task.Delay(1000));

            completed.Should().BeSameAs(state.BothStarted);

            state.ReleaseAll();
            await runTask;
        }
        finally
        {
            provider.Dispose();
        }
    }

    private abstract class TestEnricherBase : IEnricher
    {
        private readonly IList<string> _log;

        protected TestEnricherBase(IList<string> log)
        {
            _log = log;
        }

        public abstract EnricherType EnricherType { get; }

        public abstract Type[] Dependencies { get; }

        public Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default)
        {
            _log.Add(GetType().Name);
            return Task.CompletedTask;
        }
    }

    private sealed class AlphaEnricher : TestEnricherBase
    {
        public AlphaEnricher(IList<string> log) : base(log)
        {
        }

        public override EnricherType EnricherType => EnricherType.Analyze;

        public override Type[] Dependencies => Array.Empty<Type>();
    }

    private sealed class BravoEnricher : TestEnricherBase
    {
        public BravoEnricher(IList<string> log) : base(log)
        {
        }

        public override EnricherType EnricherType => EnricherType.Metadata;

        public override Type[] Dependencies => new[] { typeof(AlphaEnricher) };
    }

    private sealed class CharlieEnricher : TestEnricherBase
    {
        public CharlieEnricher(IList<string> log) : base(log)
        {
        }

        public override EnricherType EnricherType => EnricherType.Tag;

        public override Type[] Dependencies => new[] { typeof(BravoEnricher) };
    }

    private sealed class CycleAlphaEnricher : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Analyze;

        public Type[] Dependencies => new[] { typeof(CycleBravoEnricher) };

        public Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CycleBravoEnricher : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Metadata;

        public Type[] Dependencies => new[] { typeof(CycleAlphaEnricher) };

        public Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class BlockingEnricherState
    {
        private readonly int _expectedStarts;
        private readonly TaskCompletionSource<bool> _bothStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _started;

        public BlockingEnricherState(int expectedStarts)
        {
            _expectedStarts = expectedStarts;
        }

        public Task BothStarted => _bothStarted.Task;

        public Task Release => _release.Task;

        public void SignalStarted()
        {
            if (Interlocked.Increment(ref _started) == _expectedStarts)
            {
                _bothStarted.TrySetResult(true);
            }
        }

        public void ReleaseAll() => _release.TrySetResult(true);
    }

    private sealed class BlockingEnricher : IEnricher
    {
        private readonly BlockingEnricherState _state;

        public BlockingEnricher(BlockingEnricherState state)
        {
            _state = state;
        }

        public EnricherType EnricherType => EnricherType.Analyze;

        public Type[] Dependencies => Array.Empty<Type>();

        public async Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default)
        {
            _state.SignalStarted();
            await _state.Release;
        }
    }
}
