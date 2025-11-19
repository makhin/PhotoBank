using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichment;
using PhotoBank.Services.Models;
using PhotoBank.UnitTests.Helpers;

namespace PhotoBank.UnitTests.Enrichment;

[TestFixture]
public class ReEnrichmentServiceTests
{
    private PhotoBankDbContext _context;
    private Mock<IRepository<Enricher>> _enricherRepositoryMock;
    private Mock<IEnrichmentPipeline> _enrichmentPipelineMock;
    private Mock<IActiveEnricherProvider> _activeEnricherProviderMock;
    private Mock<EnricherDiffCalculator> _enricherDiffCalculatorMock;
    private ReEnrichmentService _service;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbFactory.CreateInMemory();
        _enricherRepositoryMock = new Mock<IRepository<Enricher>>();
        _enrichmentPipelineMock = new Mock<IEnrichmentPipeline>();
        _activeEnricherProviderMock = new Mock<IActiveEnricherProvider>();

        // Create a mock IServiceProvider to satisfy EnricherDiffCalculator constructor
        var serviceProviderMock = new Mock<IServiceProvider>();
        _enricherDiffCalculatorMock = new Mock<EnricherDiffCalculator>(MockBehavior.Strict, serviceProviderMock.Object);

        _service = new ReEnrichmentService(
            _context,
            _enricherRepositoryMock.Object,
            _enrichmentPipelineMock.Object,
            _activeEnricherProviderMock.Object,
            _enricherDiffCalculatorMock.Object,
            NullLogger<ReEnrichmentService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task ReEnrichPhotoAsync_WithValidPhoto_ForceRunsEnrichersAndUpdatesPhoto()
    {
        // Arrange
        var photoId = 1;
        var enricherTypes = new[] { typeof(MockEnricherA), typeof(MockEnricherB) };
        var photo = CreateTestPhoto(photoId);
        await SeedPhotoAsync(photo);

        // Setup ExpandWithDependencies for force re-run (no CalculateMissingEnrichers)
        _enricherDiffCalculatorMock
            .Setup(c => c.ExpandWithDependencies(enricherTypes))
            .Returns(enricherTypes);

        SetupEnrichmentPipelineToSucceed();

        // Act
        var result = await _service.ReEnrichPhotoAsync(photoId, enricherTypes);

        // Assert
        result.Should().BeTrue();
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(
                It.Is<Photo>(ph => ph.Id == photoId),
                It.Is<SourceDataDto>(s => s.AbsolutePath.EndsWith("test.jpg")),
                It.Is<IReadOnlyCollection<Type>>(types => types.SequenceEqual(enricherTypes)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify CalculateMissingEnrichers was NOT called (force re-run)
        _enricherDiffCalculatorMock.Verify(
            c => c.CalculateMissingEnrichers(It.IsAny<Photo>(), It.IsAny<IReadOnlyCollection<Type>>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichPhotoAsync_WithAlreadyAppliedEnrichers_ForceRunsThem()
    {
        // Arrange
        var photoId = 1;
        var enricherTypes = new[] { typeof(MockEnricherA) };
        var photo = CreateTestPhoto(photoId);

        // Mark enricher as already applied
        photo.EnrichedWithEnricherType = EnricherType.Metadata;

        await SeedPhotoAsync(photo);

        // Setup ExpandWithDependencies - should return enrichers regardless of applied status
        _enricherDiffCalculatorMock
            .Setup(c => c.ExpandWithDependencies(enricherTypes))
            .Returns(enricherTypes);

        SetupEnrichmentPipelineToSucceed();

        // Act
        var result = await _service.ReEnrichPhotoAsync(photoId, enricherTypes);

        // Assert - enricher should run even though it's already applied
        result.Should().BeTrue();
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(
                It.IsAny<Photo>(),
                It.IsAny<SourceDataDto>(),
                It.Is<IReadOnlyCollection<Type>>(types => types.Contains(typeof(MockEnricherA))),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify CalculateMissingEnrichers was NOT called (bypasses already-applied check)
        _enricherDiffCalculatorMock.Verify(
            c => c.CalculateMissingEnrichers(It.IsAny<Photo>(), It.IsAny<IReadOnlyCollection<Type>>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichPhotoAsync_WithNoEnricherTypes_ReturnsFalse()
    {
        // Arrange
        var photoId = 1;
        var enricherTypes = Array.Empty<Type>();

        // Act
        var result = await _service.ReEnrichPhotoAsync(photoId, enricherTypes);

        // Assert
        result.Should().BeFalse();
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichPhotoAsync_WithNonExistentPhoto_ReturnsFalse()
    {
        // Arrange
        var photoId = 999;
        var enricherTypes = new[] { typeof(MockEnricherA) };

        // Act
        var result = await _service.ReEnrichPhotoAsync(photoId, enricherTypes);

        // Assert
        result.Should().BeFalse();
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichPhotoAsync_WithPhotoWithoutFiles_ReturnsFalse()
    {
        // Arrange
        var photoId = 1;
        var enricherTypes = new[] { typeof(MockEnricherA) };
        var photo = CreateTestPhoto(photoId);
        photo.Files = null;

        await SeedPhotoAsync(photo);

        // Act
        var result = await _service.ReEnrichPhotoAsync(photoId, enricherTypes);

        // Assert
        result.Should().BeFalse();
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichPhotosAsync_WithMultiplePhotos_ProcessesAll()
    {
        // Arrange
        var photoIds = new[] { 1, 2, 3 };
        var enricherTypes = new[] { typeof(MockEnricherA) };

        foreach (var id in photoIds)
        {
            var photo = CreateTestPhoto(id);
            await SeedPhotoAsync(photo);
        }

        // Setup ExpandWithDependencies for force re-run
        _enricherDiffCalculatorMock
            .Setup(c => c.ExpandWithDependencies(enricherTypes))
            .Returns(enricherTypes);

        SetupEnrichmentPipelineToSucceed();

        // Act
        var result = await _service.ReEnrichPhotosAsync(photoIds, enricherTypes);

        // Assert
        result.Should().Be(3);
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Test]
    public async Task ReEnrichPhotosAsync_WithSomeFailingPhotos_ContinuesProcessing()
    {
        // Arrange
        var photoIds = new[] { 1, 2, 3 };
        var enricherTypes = new[] { typeof(MockEnricherA) };

        // Photo 1 - success
        var photo1 = CreateTestPhoto(1);
        await SeedPhotoAsync(photo1);

        // Photo 2 - not found (skip seeding)

        // Photo 3 - success
        var photo3 = CreateTestPhoto(3);
        await SeedPhotoAsync(photo3);

        // Setup ExpandWithDependencies for force re-run
        _enricherDiffCalculatorMock
            .Setup(c => c.ExpandWithDependencies(enricherTypes))
            .Returns(enricherTypes);

        SetupEnrichmentPipelineToSucceed();

        // Act
        var result = await _service.ReEnrichPhotosAsync(photoIds, enricherTypes);

        // Assert
        result.Should().Be(2); // Only 2 succeeded
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task ReEnrichPhotosAsync_WithEmptyPhotoIds_ReturnsZero()
    {
        // Arrange
        var photoIds = Array.Empty<int>();
        var enricherTypes = new[] { typeof(MockEnricherA) };

        // Act
        var result = await _service.ReEnrichPhotosAsync(photoIds, enricherTypes);

        // Assert
        result.Should().Be(0);
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichPhotosAsync_WithEmptyEnricherTypes_ReturnsZero()
    {
        // Arrange
        var photoIds = new[] { 1, 2, 3 };
        var enricherTypes = Array.Empty<Type>();

        // Act
        var result = await _service.ReEnrichPhotosAsync(photoIds, enricherTypes);

        // Assert
        result.Should().Be(0);
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichMissingAsync_WithMissingEnrichers_RunsEnrichersAndUpdatesPhoto()
    {
        // Arrange
        var photoId = 1;
        var photo = CreateTestPhoto(photoId);
        var activeEnrichers = new[] { typeof(MockEnricherA), typeof(MockEnricherB) };
        var missingEnrichers = new[] { typeof(MockEnricherB) };

        await SeedPhotoAsync(photo);
        SetupActiveEnricherProvider(activeEnrichers);
        SetupDiffCalculatorToReturnEnrichers(photo, activeEnrichers, missingEnrichers);
        SetupEnrichmentPipelineToSucceed();

        // Act
        var result = await _service.ReEnrichMissingAsync(photoId);

        // Assert
        result.Should().BeTrue();
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(
                It.Is<Photo>(ph => ph.Id == photoId),
                It.IsAny<SourceDataDto>(),
                It.Is<IReadOnlyCollection<Type>>(types => types.SequenceEqual(missingEnrichers)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ReEnrichMissingAsync_WithNoMissingEnrichers_ReturnsFalse()
    {
        // Arrange
        var photoId = 1;
        var photo = CreateTestPhoto(photoId);
        var activeEnrichers = new[] { typeof(MockEnricherA) };

        await SeedPhotoAsync(photo);
        SetupActiveEnricherProvider(activeEnrichers);
        SetupDiffCalculatorToReturnEnrichers(photo, activeEnrichers, Array.Empty<Type>());

        // Act
        var result = await _service.ReEnrichMissingAsync(photoId);

        // Assert
        result.Should().BeFalse();
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichMissingAsync_WithNonExistentPhoto_ReturnsFalse()
    {
        // Arrange
        var photoId = 999;

        // Act
        var result = await _service.ReEnrichMissingAsync(photoId);

        // Assert
        result.Should().BeFalse();
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichMissingAsync_WithNoActiveEnrichers_ReturnsFalse()
    {
        // Arrange
        var photoId = 1;
        var photo = CreateTestPhoto(photoId);

        await SeedPhotoAsync(photo);
        SetupActiveEnricherProvider(Array.Empty<Type>());

        // Act
        var result = await _service.ReEnrichMissingAsync(photoId);

        // Assert
        result.Should().BeFalse();
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichMissingBatchAsync_WithMultiplePhotos_ProcessesAll()
    {
        // Arrange
        var photoIds = new[] { 1, 2, 3 };
        var activeEnrichers = new[] { typeof(MockEnricherA) };

        SetupActiveEnricherProvider(activeEnrichers);

        foreach (var id in photoIds)
        {
            var photo = CreateTestPhoto(id);
            await SeedPhotoAsync(photo);
            SetupDiffCalculatorToReturnEnrichers(photo, activeEnrichers, activeEnrichers);
        }

        SetupEnrichmentPipelineToSucceed();

        // Act
        var result = await _service.ReEnrichMissingBatchAsync(photoIds);

        // Assert
        result.Should().Be(3);
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Test]
    public async Task ReEnrichMissingBatchAsync_WithEmptyPhotoIds_ReturnsZero()
    {
        // Arrange
        var photoIds = Array.Empty<int>();

        // Act
        var result = await _service.ReEnrichMissingBatchAsync(photoIds);

        // Assert
        result.Should().Be(0);
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichPhotosAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        var photoIds = new[] { 1, 2, 3 };
        var enricherTypes = new[] { typeof(MockEnricherA) };
        var cts = new CancellationTokenSource();

        // Setup first photo to succeed
        var photo1 = CreateTestPhoto(1);
        await SeedPhotoAsync(photo1);

        // Setup enrichment pipeline to cancel after first photo
        var callCount = 0;
        _enrichmentPipelineMock
            .Setup(p => p.RunAsync(
                It.IsAny<Photo>(),
                It.IsAny<SourceDataDto>(),
                It.IsAny<IReadOnlyCollection<Type>>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Task.CompletedTask; // First call succeeds
                }
                // Second call triggers cancellation
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });

        _enricherDiffCalculatorMock
            .Setup(c => c.ExpandWithDependencies(enricherTypes))
            .Returns(enricherTypes);

        // Setup second photo
        var photo2 = CreateTestPhoto(2);
        await SeedPhotoAsync(photo2);

        // Act & Assert
        var act = async () => await _service.ReEnrichPhotosAsync(photoIds, enricherTypes, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Verify processing stopped after cancellation (no third photo processed)
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Only first and second photos
    }

    [Test]
    public async Task ReEnrichMissingBatchAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        var photoIds = new[] { 1, 2, 3 };
        var activeEnrichers = new[] { typeof(MockEnricherA) };
        var cts = new CancellationTokenSource();

        SetupActiveEnricherProvider(activeEnrichers);

        // Setup first photo to succeed
        var photo1 = CreateTestPhoto(1);
        await SeedPhotoAsync(photo1);
        SetupDiffCalculatorToReturnEnrichers(photo1, activeEnrichers, activeEnrichers);

        // Setup enrichment pipeline to cancel after first photo
        var callCount = 0;
        _enrichmentPipelineMock
            .Setup(p => p.RunAsync(
                It.IsAny<Photo>(),
                It.IsAny<SourceDataDto>(),
                It.IsAny<IReadOnlyCollection<Type>>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Task.CompletedTask; // First call succeeds
                }
                // Second call triggers cancellation
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });

        // Setup second photo
        var photo2 = CreateTestPhoto(2);
        await SeedPhotoAsync(photo2);
        SetupDiffCalculatorToReturnEnrichers(photo2, activeEnrichers, activeEnrichers);

        // Act & Assert
        var act = async () => await _service.ReEnrichMissingBatchAsync(photoIds, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Verify processing stopped after cancellation (no third photo processed)
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Only first and second photos
    }

    // Helper methods
    private Photo CreateTestPhoto(int id)
    {
        var storage = _context.Storages.FirstOrDefault() ?? new Storage { Id = 1, Name = "Test", Folder = "/storage" };
        if (storage.Id == 0)
        {
            _context.Storages.Add(storage);
            _context.SaveChanges();
        }

        return new Photo
        {
            Id = id,
            Name = "test",
            RelativePath = "photos",
            StorageId = storage.Id,
            Storage = storage,
            Files = new List<DbContext.Models.File>
            {
                new() { Name = "test.jpg" }
            },
            EnrichedWithEnricherType = EnricherType.None
        };
    }

    private async Task SeedPhotoAsync(Photo photo)
    {
        _context.Photos.Add(photo);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private void SetupDiffCalculatorToReturnEnrichers(Photo photo, IReadOnlyCollection<Type> activeEnrichers, IReadOnlyCollection<Type> enrichersToRun)
    {
        _enricherDiffCalculatorMock
            .Setup(c => c.CalculateMissingEnrichers(It.IsAny<Photo>(), activeEnrichers))
            .Returns(enrichersToRun);

        // Setup ExpandWithDependencies to return the same enrichers (with dependencies already included in tests)
        // The actual dependency expansion logic is tested in EnricherDiffCalculatorTests
        _enricherDiffCalculatorMock
            .Setup(c => c.ExpandWithDependencies(It.IsAny<IReadOnlyCollection<Type>>()))
            .Returns<IReadOnlyCollection<Type>>(enrichers => enrichers);
    }

    private void SetupEnrichmentPipelineToSucceed()
    {
        _enrichmentPipelineMock
            .Setup(p => p.RunAsync(
                It.IsAny<Photo>(),
                It.IsAny<SourceDataDto>(),
                It.IsAny<IReadOnlyCollection<Type>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupActiveEnricherProvider(IReadOnlyCollection<Type> enrichers)
    {
        _activeEnricherProviderMock
            .Setup(p => p.GetActiveEnricherTypes(It.IsAny<IRepository<Enricher>>()))
            .Returns(enrichers);
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
        public Type[] Dependencies => new[] { typeof(MockEnricherA) };
        public Task EnrichAsync(Photo photo, SourceDataDto path, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
