using System;
using System.Collections.Generic;
using System.Linq;
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

        // Register mock enrichers
        services.AddTransient<IEnricher, MockEnricherA>();
        services.AddTransient<IEnricher, MockEnricherB>();
        services.AddTransient<IEnricher, MockEnricherC>();
        services.AddTransient<IEnricher, MockEnricherD>();

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
    public void CalculateMissingEnrichers_WhenAllEnrichersApplied_ReturnsEmpty()
    {
        // Arrange
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview | EnricherType.Metadata
        };
        var activeEnrichers = new[] { typeof(MockEnricherA), typeof(MockEnricherB) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void CalculateMissingEnrichers_WhenPartiallyApplied_ReturnsOnlyMissing()
    {
        // Arrange
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview // Only EnricherA applied
        };
        var activeEnrichers = new[] { typeof(MockEnricherA), typeof(MockEnricherB) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(typeof(MockEnricherB));
        result.Should().NotContain(typeof(MockEnricherA));
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
    public void CalculateMissingEnrichers_WithDependenciesAlreadyApplied_OnlyIncludesMissing()
    {
        // Arrange: EnricherC depends on EnricherA and EnricherB, but EnricherA is already applied
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview // EnricherA applied
        };
        var activeEnrichers = new[] { typeof(MockEnricherC) };

        // Act
        var result = _calculator.CalculateMissingEnrichers(photo, activeEnrichers);

        // Assert
        result.Should().HaveCount(2);
        result.Should().NotContain(typeof(MockEnricherA)); // Already applied
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
        // Arrange
        var photo = new Photo
        {
            EnrichedWithEnricherType = EnricherType.Preview
        };
        var activeEnrichers = new[] { typeof(MockEnricherA) };

        // Act
        var result = _calculator.NeedsEnrichment(photo, activeEnrichers);

        // Assert
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
        public Type[] Dependencies => Array.Empty<Type>();
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
}
