using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;
using File = PhotoBank.DbContext.Models.File;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class DuplicateEnricherTests
{
    private Mock<IRepository<Photo>> _mockPhotoRepository;
    private DuplicateEnricher _enricher;
    private string _tempImagePath;

    [SetUp]
    public void Setup()
    {
        _mockPhotoRepository = new Mock<IRepository<Photo>>();
        _enricher = new DuplicateEnricher(_mockPhotoRepository.Object);

        // Create a temporary test image
        _tempImagePath = Path.Combine(Path.GetTempPath(), $"test_image_{Guid.NewGuid()}.jpg");
        using var image = new MagickImage(MagickColors.Red, 100, 100);
        image.Format = MagickFormat.Jpeg;
        image.Write(_tempImagePath);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up temporary test image
        if (System.IO.File.Exists(_tempImagePath))
        {
            System.IO.File.Delete(_tempImagePath);
        }
    }

    [Test]
    public void EnricherType_ShouldReturnDuplicate()
    {
        // Act & Assert
        _enricher.EnricherType.Should().Be(EnricherType.Duplicate);
    }

    [Test]
    public void Dependencies_ShouldReturnPreviewEnricher()
    {
        // Act & Assert
        _enricher.Dependencies.Should().ContainSingle()
            .And.Contain(typeof(PreviewEnricher));
    }

    [Test]
    public void Constructor_WithNullPhotoRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DuplicateEnricher(null));
    }

    [Test]
    public async Task EnrichAsync_ComputesImageHash()
    {
        // Arrange
        var storage = new Storage { Id = 1, Folder = Path.GetTempPath() };
        var photo = new Photo { Storage = storage };
        var sourceData = new SourceDataDto
        {
            AbsolutePath = _tempImagePath,
            PreviewImage = new MagickImage(MagickColors.Blue, 50, 50)
        };

        _mockPhotoRepository
            .Setup(r => r.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Photo, bool>>>()))
            .Returns(Enumerable.Empty<Photo>().AsQueryable());

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ImageHash.Should().NotBeNullOrEmpty();
        photo.ImageHash.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Test]
    public async Task EnrichAsync_SetsNameAndRelativePath()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "testfolder");
        System.IO.Directory.CreateDirectory(tempDir);
        var testFile = Path.Combine(tempDir, "testphoto.jpg");
        using (var image = new MagickImage(MagickColors.Red, 100, 100))
        {
            image.Write(testFile);
        }

        var storage = new Storage { Id = 1, Folder = Path.GetTempPath() };
        var photo = new Photo { Storage = storage };
        var sourceData = new SourceDataDto
        {
            AbsolutePath = testFile,
            PreviewImage = new MagickImage(MagickColors.Blue, 50, 50)
        };

        _mockPhotoRepository
            .Setup(r => r.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Photo, bool>>>()))
            .Returns(Enumerable.Empty<Photo>().AsQueryable());

        try
        {
            // Act
            await _enricher.EnrichAsync(photo, sourceData);

            // Assert
            photo.Name.Should().Be("testphoto");
            photo.RelativePath.Should().Be("testfolder");
        }
        finally
        {
            // Cleanup
            if (System.IO.File.Exists(testFile))
                System.IO.File.Delete(testFile);
            if (System.IO.Directory.Exists(tempDir))
                System.IO.Directory.Delete(tempDir);
        }
    }

    [Test]
    public async Task EnrichAsync_CreatesFilesCollection()
    {
        // Arrange
        var storage = new Storage { Id = 1, Folder = Path.GetTempPath() };
        var photo = new Photo { Storage = storage };
        var sourceData = new SourceDataDto
        {
            AbsolutePath = _tempImagePath,
            PreviewImage = new MagickImage(MagickColors.Blue, 50, 50)
        };

        _mockPhotoRepository
            .Setup(r => r.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Photo, bool>>>()))
            .Returns(Enumerable.Empty<Photo>().AsQueryable());

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.Files.Should().NotBeNull()
            .And.ContainSingle();

        var file = photo.Files.First();
        file.StorageId.Should().Be(storage.Id);
        file.Name.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task EnrichAsync_WhenNoDuplicate_DoesNotSetDuplicateInfo()
    {
        // Arrange
        var storage = new Storage { Id = 1, Folder = Path.GetTempPath() };
        var photo = new Photo { Storage = storage };
        var sourceData = new SourceDataDto
        {
            AbsolutePath = _tempImagePath,
            PreviewImage = new MagickImage(MagickColors.Blue, 50, 50)
        };

        _mockPhotoRepository
            .Setup(r => r.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Photo, bool>>>()))
            .Returns(Enumerable.Empty<Photo>().AsQueryable());

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        sourceData.DuplicatePhotoId.Should().BeNull();
        sourceData.DuplicatePhotoInfo.Should().BeNull();
    }

    [Test]
    public async Task EnrichAsync_WhenDuplicateFound_SetsDuplicateInfo()
    {
        // Arrange
        var storage = new Storage { Id = 1, Name = "TestStorage", Folder = Path.GetTempPath() };
        var photo = new Photo { Storage = storage };
        var sourceData = new SourceDataDto
        {
            AbsolutePath = _tempImagePath,
            PreviewImage = new MagickImage(MagickColors.Blue, 50, 50)
        };

        var existingPhoto = new Photo
        {
            Id = 42,
            ImageHash = "abc123",
            Storage = new Storage { Name = "ExistingStorage" },
            Files = new List<File>
            {
                new File { RelativePath = "photos/2024" }
            }
        };

        _mockPhotoRepository
            .Setup(r => r.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Photo, bool>>>()))
            .Returns(new[] { existingPhoto }.AsQueryable());

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        sourceData.DuplicatePhotoId.Should().Be(42);
        sourceData.DuplicatePhotoInfo.Should().Contain("Photo #42")
            .And.Contain("ExistingStorage")
            .And.Contain("photos/2024");
    }

    [Test]
    public async Task EnrichAsync_WhenPreviewImageIsNull_DoesNotComputeHash()
    {
        // Arrange
        var storage = new Storage { Id = 1, Folder = Path.GetTempPath() };
        var photo = new Photo { Storage = storage };
        var sourceData = new SourceDataDto
        {
            AbsolutePath = _tempImagePath,
            PreviewImage = null  // No preview image
        };

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        photo.ImageHash.Should().BeNullOrEmpty();

        // Repository should not be called since there's no hash to search for
        _mockPhotoRepository.Verify(
            r => r.GetByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Photo, bool>>>()),
            Times.Never);
    }
}
