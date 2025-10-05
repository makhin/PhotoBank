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
        var pipeline = new EnrichmentPipeline(
            provider,
            new[] { typeof(AlphaEnricher), typeof(BravoEnricher), typeof(CharlieEnricher) },
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
                new[] { typeof(AlphaEnricher), typeof(CharlieEnricher) },
                CancellationToken.None);
        }
        finally
        {
            provider.Dispose();
        }

        photo.EnrichedWithEnricherType.Should().Be(EnricherType.Analyze | EnricherType.Tag);
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
}
