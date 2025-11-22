using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Enrichment;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;

namespace PhotoBank.UnitTests.Enrichment;

[TestFixture]
public class EnricherDiffCalculatorTests
{
    private ServiceProvider _serviceProvider;
    private EnricherDiffCalculator _calculator;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        // Register mock enrichers under their concrete types
        // (required for GetRequiredService to resolve them by Type)
        services.AddTransient<MockEnricherA>();
        services.AddTransient<MockEnricherB>();
        services.AddTransient<MockEnricherC>();
        services.AddTransient<MockEnricherD>();
        services.AddTransient<MockEnricherE>();
        services.AddTransient<MockEnricherF>();

        _serviceProvider = services.BuildServiceProvider();
        _calculator = new EnricherDiffCalculator(_serviceProvider);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public void CalculateMissingEnrichers_WhenNoEnrichersApplied_ReturnsAllActiveEnrichers()
    {
        // Arrange
        var photo = new Photo { EnrichedWithEnricherType = EnricherType.None };
        var activeEnrichers = new[] { typeof(MockEnricherA), typeof(MockEnricherB) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(typeof(MockEnricherA));
        result.Should().Contain(typeof(MockEnricherB));
    }

    [Test]
    public void CalculateMissingEnrichers_WhenAllNonDataProviderEnrichersApplied_ReturnsEmpty()
    {
        // Arrange: Use only non-data provider enrichers that are already applied
        // Preview, Metadata and Face are all already applied
        // Even though Metadata depends on Preview, it's already applied so dependencies aren't checked
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview | EnricherType.Metadata | EnricherType.Face
        };
        var activeEnrichers = new[] { typeof(MockEnricherB), typeof(MockEnricherC) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        // Both enrichers are already applied and not data providers, so nothing to run
        // (dependencies are not checked when enricher is already applied)
        result.Should().BeEmpty();
    }

    [Test]
    public void CalculateMissingEnrichers_WhenPartiallyApplied_ReturnsOnlyMissing()
    {
        // Arrange: Preview already applied, but it's a data provider
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview // Only EnricherA applied
        };
        var activeEnrichers = new[] { typeof(MockEnricherA), typeof(MockEnricherB) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        // Preview is a data provider, so it's re-run even though already applied
        // Metadata is not applied, so it's included
        result.Should().HaveCount(2);
        result.Should().Contain(typeof(MockEnricherA)); // Preview (data provider, always included)
        result.Should().Contain(typeof(MockEnricherB)); // Metadata (not applied yet)
    }

    [Test]
    public void CalculateMissingEnrichers_WithDependencies_IncludesDependencies()
    {
        // Arrange: EnricherC depends on EnricherA and EnricherB
        var photo = new Photo { EnrichedWithEnricherType = EnricherType.None };
        var activeEnrichers = new[] { typeof(MockEnricherC) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(typeof(MockEnricherA)); // Dependency
        result.Should().Contain(typeof(MockEnricherB)); // Dependency
        result.Should().Contain(typeof(MockEnricherC)); // Main enricher
    }

    [Test]
    public void CalculateMissingEnrichers_WithDependenciesAlreadyApplied_IncludesDataProviderDependency()
    {
        // Arrange: EnricherC depends on EnricherA and EnricherB, EnricherA (Preview) is already applied
        // But EnricherA is a data provider, so it must still be included
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview // EnricherA applied (but it's a data provider)
        };
        var activeEnrichers = new[] { typeof(MockEnricherC) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert - EnricherA is included because it's a data provider (Preview)
        result.Should().HaveCount(3);
        result.Should().Contain(typeof(MockEnricherA)); // Data provider - always included
        result.Should().Contain(typeof(MockEnricherB)); // Missing dependency
        result.Should().Contain(typeof(MockEnricherC)); // Main enricher
    }

    [Test]
    public void CalculateMissingEnrichers_WithChainedDependencies_ResolvesAll()
    {
        // Arrange: EnricherD depends on EnricherC, which depends on EnricherA and EnricherB
        var photo = new Photo { EnrichedWithEnricherType = EnricherType.None };
        var activeEnrichers = new[] { typeof(MockEnricherD) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(typeof(MockEnricherA));
        result.Should().Contain(typeof(MockEnricherB));
        result.Should().Contain(typeof(MockEnricherC));
        result.Should().Contain(typeof(MockEnricherD));
    }

    [Test]
    public void NeedsEnrichment_WhenMissingEnrichers_ReturnsTrue()
    {
        // Arrange
        var photo = new Photo { EnrichedWithEnricherType = EnricherType.None };
        var activeEnrichers = new[] { typeof(MockEnricherA) };

        // Act
        var result = _calculator.NeedsEnrichment(photo, activeEnrichers);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void NeedsEnrichment_WhenNoMissingEnrichers_ReturnsFalse()
    {
        // Arrange - use non-data-provider enrichers (Metadata and Face)
        // to test the case where all enrichers are already applied
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Metadata | EnricherType.Face
        };
        // MockEnricherB is Metadata, MockEnricherC is Face - both non-data-providers
        var activeEnrichers = new[] { typeof(MockEnricherB), typeof(MockEnricherC) };

        // Act
        var result = _calculator.NeedsEnrichment(photo, activeEnrichers);

        // Assert - no missing enrichers when all non-data-provider enrichers are applied
        // Note: Data provider dependencies (Preview) will be included but the main enrichers
        // themselves are already applied, so NeedsEnrichment checks if any are missing
        result.Should().BeFalse();
    }

    [Test]
    public void GetAppliedEnrichers_ReturnsOnlyAppliedEnrichers()
    {
        // Arrange
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview | EnricherType.Face
        };
        var allEnrichers = new[]
        {
            typeof(MockEnricherA), // Preview
            typeof(MockEnricherB), // Metadata
            typeof(MockEnricherC), // Face
            typeof(MockEnricherD)  // Tag
        };

        // Act
        var result = _calculator.GetAppliedEnrichers(photo, allEnrichers);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(typeof(MockEnricherA)); // Preview applied
        result.Should().Contain(typeof(MockEnricherC)); // Face applied
        result.Should().NotContain(typeof(MockEnricherB));
        result.Should().NotContain(typeof(MockEnricherD));
    }

    [Test]
    public void CalculateMissingEnrichers_WithNullPhoto_ThrowsArgumentNullException()
    {
        // Arrange
        var activeEnrichers = new[] { typeof(MockEnricherA) };

        // Act
        Action act = () => _calculator.CalculateMissingEnrichers(null, activeEnrichers);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void CalculateMissingEnrichers_WithEmptyActiveEnrichers_ReturnsEmpty()
    {
        // Arrange
        var photo = new Photo { EnrichedWithEnricherType = EnricherType.None };
        var activeEnrichers = Array.Empty<Type>();

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void CalculateMissingEnrichers_WithCyclicDependencies_ThrowsInvalidOperationException()
    {
        // Arrange: MockEnricherE depends on MockEnricherF, which depends on MockEnricherE (cycle)
        var photo = new Photo { EnrichedWithEnricherType = EnricherType.None };
        var activeEnrichers = new[] { typeof(MockEnricherE) };

        // Act
        Action act = () => _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cycle*");
    }

    [Test]
    public void CalculateMissingEnrichers_WithCyclicDependencies_DirectCycle_ThrowsInvalidOperationException()
    {
        // Arrange: MockEnricherF also has a direct cycle
        var photo = new Photo { EnrichedWithEnricherType = EnricherType.None };
        var activeEnrichers = new[] { typeof(MockEnricherF) };

        // Act
        Action act = () => _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cycle*");
    }

    [Test]
    public void CalculateMissingEnrichers_DataProviderEnricherAlreadyApplied_StillIncludedAsDependency()
    {
        // Arrange: PreviewEnricher (data provider) already applied
        // MockEnricherB (Metadata) depends on PreviewEnricher
        // PreviewEnricher should be included even though already applied
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview // PreviewEnricher already applied
        };
        var activeEnrichers = new[] { typeof(MockEnricherB) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        // Even though Preview was already applied, it should be included as a data provider
        result.Should().Contain(typeof(MockEnricherA)); // PreviewEnricher
        result.Should().Contain(typeof(MockEnricherB)); // MetadataEnricher
    }

    [Test]
    public void CalculateMissingEnrichers_NonDataProviderEnricherAlreadyApplied_NotIncludedAsDependency()
    {
        // Arrange: MetadataEnricher (NOT a data provider) already applied
        // MockEnricherC (Face) depends on both Preview and Metadata
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview | EnricherType.Metadata
        };
        var activeEnrichers = new[] { typeof(MockEnricherC) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        result.Should().Contain(typeof(MockEnricherA)); // PreviewEnricher (data provider, always included)
        result.Should().NotContain(typeof(MockEnricherB)); // MetadataEnricher (not data provider, skip)
        result.Should().Contain(typeof(MockEnricherC)); // FaceEnricher
    }

    [Test]
    public void ExpandWithDependencies_WithSingleEnricher_IncludesAllDependencies()
    {
        // Arrange: MockEnricherC depends on MockEnricherA and MockEnricherB
        var enrichers = new[] { typeof(MockEnricherC) };

        // Act
        var result = _calculator.ExpandWithDependencies(enrichers);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(typeof(MockEnricherA)); // Dependency
        result.Should().Contain(typeof(MockEnricherB)); // Dependency
        result.Should().Contain(typeof(MockEnricherC)); // Main enricher
    }

    [Test]
    public void ExpandWithDependencies_WithMultipleEnrichers_IncludesAllUniqueDependencies()
    {
        // Arrange: Both MockEnricherC and MockEnricherD depend on MockEnricherA (shared dependency)
        var enrichers = new[] { typeof(MockEnricherC), typeof(MockEnricherD) };

        // Act
        var result = _calculator.ExpandWithDependencies(enrichers);

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(typeof(MockEnricherA)); // Shared dependency
        result.Should().Contain(typeof(MockEnricherB)); // Dependency of C
        result.Should().Contain(typeof(MockEnricherC));
        result.Should().Contain(typeof(MockEnricherD));
    }

    [Test]
    public void ExpandWithDependencies_WithEnricherWithoutDependencies_ReturnsOnlyThatEnricher()
    {
        // Arrange: MockEnricherA has no dependencies
        var enrichers = new[] { typeof(MockEnricherA) };

        // Act
        var result = _calculator.ExpandWithDependencies(enrichers);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(typeof(MockEnricherA));
    }

    [Test]
    public void ExpandWithDependencies_WithEmptyList_ReturnsEmpty()
    {
        // Arrange
        var enrichers = Array.Empty<Type>();

        // Act
        var result = _calculator.ExpandWithDependencies(enrichers);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ExpandWithDependencies_WithChainedDependencies_IncludesEntireChain()
    {
        // Arrange: MockEnricherD depends on MockEnricherC, which depends on MockEnricherA and MockEnricherB
        var enrichers = new[] { typeof(MockEnricherD) };

        // Act
        var result = _calculator.ExpandWithDependencies(enrichers);

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(typeof(MockEnricherA)); // Transitive dependency
        result.Should().Contain(typeof(MockEnricherB)); // Transitive dependency
        result.Should().Contain(typeof(MockEnricherC)); // Direct dependency
        result.Should().Contain(typeof(MockEnricherD)); // Main enricher
    }

    [Test]
    public void ExpandWithDependencies_WithCyclicDependencies_ThrowsInvalidOperationException()
    {
        // Arrange: MockEnricherE and MockEnricherF have cyclic dependencies
        var enrichers = new[] { typeof(MockEnricherE) };

        // Act
        Action act = () => _calculator.ExpandWithDependencies(enrichers);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cycle*");
    }

    [Test]
    public void ExpandWithDependencies_MissingEnrichersPlusExpand_ResolvesForPipeline()
    {
        // Arrange: This simulates the real-world scenario where:
        // 1. Photo has dependency A already applied
        // 2. CalculateMissingEnrichers returns only [B] (missing)
        // 3. ExpandWithDependencies should add A back for pipeline
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview // MockEnricherA already applied
        };
        var activeEnrichers = new[] { typeof(MockEnricherB) };

        // Act - simulate the two-step process in ReEnrichmentService
        var missingEnrichers = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);
        var enrichersForPipeline = _calculator.ExpandWithDependencies(missingEnrichers);

        // Assert
        // missingEnrichers should contain Preview (data provider) and Metadata (missing)
        missingEnrichers.Should().Contain(typeof(MockEnricherA)); // Preview (data provider)
        missingEnrichers.Should().Contain(typeof(MockEnricherB)); // Metadata (missing)

        // enrichersForPipeline should be the same since both are already in the list
        enrichersForPipeline.Should().HaveCount(2);
        enrichersForPipeline.Should().Contain(typeof(MockEnricherA));
        enrichersForPipeline.Should().Contain(typeof(MockEnricherB));
    }

    [Test]
    public void ExpandWithDependencies_NonDataProviderDependencyAlreadyApplied_StillIncluded()
    {
        // Arrange: This is the key fix - when MockEnricherB is already applied but not a data provider,
        // CalculateMissingEnrichers won't include it, but ExpandWithDependencies must add it back
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview | EnricherType.Metadata // Both already applied
        };
        var activeEnrichers = new[] { typeof(MockEnricherC) }; // MockEnricherC depends on A and B

        // Act
        var missingEnrichers = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);
        var enrichersForPipeline = _calculator.ExpandWithDependencies(missingEnrichers);

        // Assert
        // missingEnrichers should only contain Preview (data provider) and Face (missing)
        // but NOT Metadata (already applied, not data provider)
        missingEnrichers.Should().Contain(typeof(MockEnricherA)); // Preview (data provider)
        missingEnrichers.Should().NotContain(typeof(MockEnricherB)); // Metadata (already applied, not data provider)
        missingEnrichers.Should().Contain(typeof(MockEnricherC)); // Face (missing)

        // enrichersForPipeline MUST include Metadata for pipeline's topological sort to work
        enrichersForPipeline.Should().HaveCount(3);
        enrichersForPipeline.Should().Contain(typeof(MockEnricherA)); // Preview
        enrichersForPipeline.Should().Contain(typeof(MockEnricherB)); // Metadata (added back by ExpandWithDependencies!)
        enrichersForPipeline.Should().Contain(typeof(MockEnricherC)); // Face
    }

    // Mock enrichers for testing
    private class MockEnricherA : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Preview;
        public Type[] Dependencies => Array.Empty<Type>();
        public Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class MockEnricherB : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Metadata;
        public Type[] Dependencies => new[] { typeof(MockEnricherA) }; // Depends on Preview
        public Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class MockEnricherC : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Face;
        public Type[] Dependencies => new[] { typeof(MockEnricherA), typeof(MockEnricherB) };
        public Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class MockEnricherD : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Tag;
        public Type[] Dependencies => new[] { typeof(MockEnricherC) };
        public Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    // Mock enrichers with cyclic dependencies for testing cycle detection
    private class MockEnricherE : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Color;
        public Type[] Dependencies => new[] { typeof(MockEnricherF) }; // Depends on F
        public Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class MockEnricherF : IEnricher
    {
        public EnricherType EnricherType => EnricherType.Category;
        public Type[] Dependencies => new[] { typeof(MockEnricherE) }; // Depends on E (creates cycle)
        public Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
