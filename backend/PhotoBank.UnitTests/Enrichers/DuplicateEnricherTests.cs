using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Models;
using File = PhotoBank.DbContext.Models.File;

namespace PhotoBank.UnitTests.Enrichers;

[TestFixture]
public class DuplicateEnricherTests
{
    private PhotoBankDbContext _dbContext;
    private ServiceProvider _serviceProvider;
    private IRepository<Photo> _photoRepository;
    private DuplicateEnricher _enricher;
    private string _tempImagePath;

    [SetUp]
    public void Setup()
    {
        _dbContext = TestDbFactory.CreateInMemory();

        var services = new ServiceCollection();
        services.AddSingleton(_dbContext);
        _serviceProvider = services.BuildServiceProvider();

        _photoRepository = new Repository<Photo>(_serviceProvider);
        _enricher = new DuplicateEnricher(_photoRepository);

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

        _serviceProvider?.Dispose();
        _dbContext?.Dispose();
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
        _dbContext.Storages.Add(storage);
        await _dbContext.SaveChangesAsync();

        var photo = new Photo { Storage = storage };
        var sourceData = new SourceDataDto
        {
            AbsolutePath = _tempImagePath,
            PreviewImage = new MagickImage(MagickColors.Blue, 50, 50)
        };

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
        _dbContext.Storages.Add(storage);
        await _dbContext.SaveChangesAsync();

        var photo = new Photo { Storage = storage };
        var sourceData = new SourceDataDto
        {
            AbsolutePath = testFile,
            PreviewImage = new MagickImage(MagickColors.Blue, 50, 50)
        };

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
        _dbContext.Storages.Add(storage);
        await _dbContext.SaveChangesAsync();

        var photo = new Photo { Storage = storage };
        var sourceData = new SourceDataDto
        {
            AbsolutePath = _tempImagePath,
            PreviewImage = new MagickImage(MagickColors.Blue, 50, 50)
        };

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
        _dbContext.Storages.Add(storage);
        await _dbContext.SaveChangesAsync();

        var photo = new Photo { Storage = storage };
        var sourceData = new SourceDataDto
        {
            AbsolutePath = _tempImagePath,
            PreviewImage = new MagickImage(MagickColors.Blue, 50, 50)
        };

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
        var existingStorage = new Storage { Id = 1, Name = "ExistingStorage", Folder = "/existing" };
        var storage = new Storage { Id = 2, Name = "TestStorage", Folder = Path.GetTempPath() };
        _dbContext.Storages.AddRange(existingStorage, storage);
        await _dbContext.SaveChangesAsync();

        // Add existing photo with specific hash to database
        var existingPhoto = new Photo
        {
            ImageHash = "abc123",
            Storage = existingStorage,
            Files = new List<File>
            {
                new File { StorageId = existingStorage.Id, RelativePath = "photos/2024", Name = "existing.jpg" }
            }
        };
        _dbContext.Photos.Add(existingPhoto);
        await _dbContext.SaveChangesAsync();

        var photo = new Photo { Storage = storage };

        // Create image that will produce the same hash
        using var previewImage = new MagickImage(MagickColors.Blue, 50, 50);
        var sourceData = new SourceDataDto
        {
            AbsolutePath = _tempImagePath,
            PreviewImage = previewImage
        };

        // Compute hash and update existing photo to match
        var computedHash = ImageHashHelper.ComputeHash(previewImage);
        existingPhoto.ImageHash = computedHash;
        await _dbContext.SaveChangesAsync();

        // Act
        await _enricher.EnrichAsync(photo, sourceData);

        // Assert
        sourceData.DuplicatePhotoId.Should().Be(existingPhoto.Id);
        sourceData.DuplicatePhotoInfo.Should().Contain($"Photo #{existingPhoto.Id}")
            .And.Contain("ExistingStorage")
            .And.Contain("photos/2024");
    }

    [Test]
    public async Task EnrichAsync_WhenPreviewImageIsNull_DoesNotComputeHash()
    {
        // Arrange
        var storage = new Storage { Id = 1, Folder = Path.GetTempPath() };
        _dbContext.Storages.Add(storage);
        await _dbContext.SaveChangesAsync();

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

        // Duplicate search should not happen since there's no hash
        sourceData.DuplicatePhotoId.Should().BeNull();
    }
}
