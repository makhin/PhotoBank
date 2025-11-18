using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
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
    private Mock<IRepository<Photo>> _photoRepositoryMock;
    private Mock<IRepository<Enricher>> _enricherRepositoryMock;
    private Mock<IEnrichmentPipeline> _enrichmentPipelineMock;
    private Mock<IActiveEnricherProvider> _activeEnricherProviderMock;
    private Mock<EnricherDiffCalculator> _enricherDiffCalculatorMock;
    private ReEnrichmentService _service;

    [SetUp]
    public void SetUp()
    {
        _photoRepositoryMock = new Mock<IRepository<Photo>>();
        _enricherRepositoryMock = new Mock<IRepository<Enricher>>();
        _enrichmentPipelineMock = new Mock<IEnrichmentPipeline>();
        _activeEnricherProviderMock = new Mock<IActiveEnricherProvider>();

        // Create a mock IServiceProvider to satisfy EnricherDiffCalculator constructor
        var serviceProviderMock = new Mock<IServiceProvider>();
        _enricherDiffCalculatorMock = new Mock<EnricherDiffCalculator>(MockBehavior.Strict, serviceProviderMock.Object);

        _service = new ReEnrichmentService(
            _photoRepositoryMock.Object,
            _enricherRepositoryMock.Object,
            _enrichmentPipelineMock.Object,
            _activeEnricherProviderMock.Object,
            _enricherDiffCalculatorMock.Object,
            NullLogger<ReEnrichmentService>.Instance);
    }

    [Test]
    public async Task ReEnrichPhotoAsync_WithValidPhoto_RunsEnrichersAndUpdatesPhoto()
    {
        // Arrange
        var photoId = 1;
        var enricherTypes = new[] { typeof(MockEnricherA), typeof(MockEnricherB) };
        var photo = CreateTestPhoto(photoId);

        SetupPhotoRepositoryToReturn(photoId, photo);
        SetupDiffCalculatorToReturnEnrichers(photo, enricherTypes, enricherTypes);
        SetupEnrichmentPipelineToSucceed();
        SetupPhotoRepositoryUpdateToSucceed();

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
        _photoRepositoryMock.Verify(p => p.UpdateAsync(photo), Times.Once);
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

        SetupPhotoRepositoryToReturn(photoId, null);

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

        SetupPhotoRepositoryToReturn(photoId, photo);

        // Act
        var result = await _service.ReEnrichPhotoAsync(photoId, enricherTypes);

        // Assert
        result.Should().BeFalse();
        _enrichmentPipelineMock.Verify(
            p => p.RunAsync(It.IsAny<Photo>(), It.IsAny<SourceDataDto>(), It.IsAny<IReadOnlyCollection<Type>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ReEnrichPhotoAsync_WhenNoEnrichersNeedToRun_ReturnsTrue()
    {
        // Arrange
        var photoId = 1;
        var enricherTypes = new[] { typeof(MockEnricherA) };
        var photo = CreateTestPhoto(photoId);

        SetupPhotoRepositoryToReturn(photoId, photo);
        SetupDiffCalculatorToReturnEnrichers(photo, enricherTypes, Array.Empty<Type>());

        // Act
        var result = await _service.ReEnrichPhotoAsync(photoId, enricherTypes);

        // Assert
        result.Should().BeTrue();
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
            SetupPhotoRepositoryToReturn(id, photo);
            SetupDiffCalculatorToReturnEnrichers(photo, enricherTypes, enricherTypes);
        }

        SetupEnrichmentPipelineToSucceed();
        SetupPhotoRepositoryUpdateToSucceed();

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
        SetupPhotoRepositoryToReturn(1, photo1);
        SetupDiffCalculatorToReturnEnrichers(photo1, enricherTypes, enricherTypes);

        // Photo 2 - not found
        SetupPhotoRepositoryToReturn(2, null);

        // Photo 3 - success
        var photo3 = CreateTestPhoto(3);
        SetupPhotoRepositoryToReturn(3, photo3);
        SetupDiffCalculatorToReturnEnrichers(photo3, enricherTypes, enricherTypes);

        SetupEnrichmentPipelineToSucceed();
        SetupPhotoRepositoryUpdateToSucceed();

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

        SetupPhotoRepositoryToReturn(photoId, photo);
        SetupActiveEnricherProvider(activeEnrichers);
        SetupDiffCalculatorToReturnEnrichers(photo, activeEnrichers, missingEnrichers);
        SetupEnrichmentPipelineToSucceed();
        SetupPhotoRepositoryUpdateToSucceed();

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
        _photoRepositoryMock.Verify(p => p.UpdateAsync(photo), Times.Once);
    }

    [Test]
    public async Task ReEnrichMissingAsync_WithNoMissingEnrichers_ReturnsFalse()
    {
        // Arrange
        var photoId = 1;
        var photo = CreateTestPhoto(photoId);
        var activeEnrichers = new[] { typeof(MockEnricherA) };

        SetupPhotoRepositoryToReturn(photoId, photo);
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

        SetupPhotoRepositoryToReturn(photoId, null);

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

        SetupPhotoRepositoryToReturn(photoId, photo);
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
            SetupPhotoRepositoryToReturn(id, photo);
            SetupDiffCalculatorToReturnEnrichers(photo, activeEnrichers, activeEnrichers);
        }

        SetupEnrichmentPipelineToSucceed();
        SetupPhotoRepositoryUpdateToSucceed();

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

    // Helper methods
    private Photo CreateTestPhoto(int id)
    {
        return new Photo
        {
            Id = id,
            Name = "test",
            RelativePath = "photos",
            Storage = new Storage { Folder = "/storage" },
            Files = new List<DbContext.Models.File>
            {
                new() { Name = "test.jpg" }
            },
            EnrichedWithEnricherType = EnricherType.None
        };
    }

    private void SetupPhotoRepositoryToReturn(int photoId, Photo photo)
    {
        var queryableMock = new TestAsyncEnumerable<Photo>(
            photo != null ? new[] { photo } : Array.Empty<Photo>()).AsQueryable();

        _photoRepositoryMock.Setup(r => r.GetAll())
            .Returns(queryableMock);
    }

    private void SetupDiffCalculatorToReturnEnrichers(Photo photo, IReadOnlyCollection<Type> activeEnrichers, IReadOnlyCollection<Type> enrichersToRun)
    {
        _enricherDiffCalculatorMock
            .Setup(c => c.CalculateMissingEnrichers(photo, activeEnrichers))
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

    private void SetupPhotoRepositoryUpdateToSucceed()
    {
        _photoRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Photo>()))
            .ReturnsAsync((Photo p) => p);
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
