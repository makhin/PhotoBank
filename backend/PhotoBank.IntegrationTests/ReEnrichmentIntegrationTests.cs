using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Moq;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.DependencyInjection;
using PhotoBank.Repositories;
using PhotoBank.Services.Enrichment;
using PhotoBank.UnitTests;

namespace PhotoBank.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class ReEnrichmentIntegrationTests
{
    private ServiceProvider _provider = null!;
    private PhotoBankDbContext _context = null!;
    private string _testStorageFolder = null!;
    private string _testFilePath = null!;

    [SetUp]
    public async Task SetUp()
    {
        // Create test storage folder and file
        _testStorageFolder = Path.Combine(Path.GetTempPath(), "PhotoBankTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_testStorageFolder);

        var photoFolder = Path.Combine(_testStorageFolder, "photos");
        Directory.CreateDirectory(photoFolder);

        _testFilePath = Path.Combine(photoFolder, "test.jpg");
        await File.WriteAllBytesAsync(_testFilePath, new byte[] { 0xFF, 0xD8, 0xFF }); // Minimal JPEG header

        // Setup in-memory database
        _context = TestDbFactory.CreateInMemory();

        var services = new ServiceCollection();
        services.AddSingleton(_context);
        services.AddLogging();
        services.AddSingleton<IMinioClient>(Mock.Of<IMinioClient>());

        // Add PhotoBank core services
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddSingleton<IActiveEnricherProvider, ActiveEnricherProvider>();
        services.AddScoped<EnricherDiffCalculator>();
        services.AddScoped<IReEnrichmentService, ReEnrichmentService>();

        // Add enrichment pipeline with test enrichers
        services.AddEnrichmentPipeline(
            opts =>
            {
                opts.LogTimings = false;
                opts.ContinueOnError = true;
            },
            typeof(TestEnricherA).Assembly);

        _provider = services.BuildServiceProvider();

        // Seed test data
        await SeedTestDataAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_provider != null)
        {
            await _provider.DisposeAsync();
        }

        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        if (Directory.Exists(_testStorageFolder))
        {
            Directory.Delete(_testStorageFolder, recursive: true);
        }
    }

    [Test]
    public async Task ReEnrichPhotoAsync_WithSpecificEnrichers_UpdatesPhotoCorrectly()
    {
        // Arrange
        var service = _provider.GetRequiredService<IReEnrichmentService>();
        var photoRepo = _provider.GetRequiredService<IRepository<Photo>>();

        var photo = await photoRepo.GetAll().FirstAsync();
        var initialEnrichedType = photo.EnrichedWithEnricherType;

        // Act - re-run specific enrichers
        var enricherTypes = new[] { typeof(TestEnricherA), typeof(TestEnricherB) };
        var result = await service.ReEnrichPhotoAsync(photo.Id, enricherTypes);

        // Assert
        result.Should().BeTrue();

        // Reload photo to check updates
        _context.ChangeTracker.Clear();
        var updatedPhoto = await photoRepo.GetAsync(photo.Id);

        updatedPhoto.EnrichedWithEnricherType.Should().HaveFlag(EnricherType.Metadata);
        updatedPhoto.EnrichedWithEnricherType.Should().HaveFlag(EnricherType.Tag);
    }

    [Test]
    public async Task ReEnrichPhotoAsync_WithNonExistentPhoto_ReturnsFalse()
    {
        // Arrange
        var service = _provider.GetRequiredService<IReEnrichmentService>();
        var enricherTypes = new[] { typeof(TestEnricherA) };

        // Act
        var result = await service.ReEnrichPhotoAsync(999, enricherTypes);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task ReEnrichPhotosAsync_WithMultiplePhotos_ProcessesAllSuccessfully()
    {
        // Arrange
        var service = _provider.GetRequiredService<IReEnrichmentService>();
        var photoRepo = _provider.GetRequiredService<IRepository<Photo>>();

        var photoIds = await photoRepo.GetAll().Select(p => p.Id).ToListAsync();
        var enricherTypes = new[] { typeof(TestEnricherA) };

        // Act
        var processedCount = await service.ReEnrichPhotosAsync(photoIds, enricherTypes);

        // Assert
        processedCount.Should().Be(photoIds.Count);

        // Verify all photos were updated
        _context.ChangeTracker.Clear();
        var photos = await photoRepo.GetAll().ToListAsync();
        foreach (var photo in photos)
        {
            photo.EnrichedWithEnricherType.Should().HaveFlag(EnricherType.Metadata);
        }
    }

    [Test]
    public async Task ReEnrichMissingAsync_WithMissingEnrichers_AppliesThem()
    {
        // Arrange
        var service = _provider.GetRequiredService<IReEnrichmentService>();
        var photoRepo = _provider.GetRequiredService<IRepository<Photo>>();
        var enricherRepo = _provider.GetRequiredService<IRepository<Enricher>>();

        // Setup active enrichers
        await enricherRepo.InsertAsync(new Enricher
        {
            Name = nameof(TestEnricherA),
            IsActive = true
        });
        await enricherRepo.InsertAsync(new Enricher
        {
            Name = nameof(TestEnricherB),
            IsActive = true
        });

        var photo = await photoRepo.GetAll().FirstAsync();
        photo.EnrichedWithEnricherType = EnricherType.None; // Reset to simulate missing enrichers
        await photoRepo.UpdateAsync(photo);

        // Act
        var result = await service.ReEnrichMissingAsync(photo.Id);

        // Assert
        result.Should().BeTrue();

        // Verify enrichers were applied
        _context.ChangeTracker.Clear();
        var updatedPhoto = await photoRepo.GetAsync(photo.Id);
        updatedPhoto.EnrichedWithEnricherType.Should().HaveFlag(EnricherType.Metadata);
        updatedPhoto.EnrichedWithEnricherType.Should().HaveFlag(EnricherType.Tag);
    }

    [Test]
    public async Task ReEnrichMissingAsync_WithAllEnrichersApplied_ReturnsFalse()
    {
        // Arrange
        var service = _provider.GetRequiredService<IReEnrichmentService>();
        var photoRepo = _provider.GetRequiredService<IRepository<Photo>>();
        var enricherRepo = _provider.GetRequiredService<IRepository<Enricher>>();

        // Setup active enrichers
        await enricherRepo.InsertAsync(new Enricher
        {
            Name = nameof(TestEnricherA),
            IsActive = true
        });

        var photo = await photoRepo.GetAll().FirstAsync();
        photo.EnrichedWithEnricherType = EnricherType.Metadata; // Already applied
        await photoRepo.UpdateAsync(photo);

        // Act
        var result = await service.ReEnrichMissingAsync(photo.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task ReEnrichMissingBatchAsync_WithMultiplePhotos_ProcessesMissingEnrichers()
    {
        // Arrange
        var service = _provider.GetRequiredService<IReEnrichmentService>();
        var photoRepo = _provider.GetRequiredService<IRepository<Photo>>();
        var enricherRepo = _provider.GetRequiredService<IRepository<Enricher>>();

        // Setup active enrichers
        await enricherRepo.InsertAsync(new Enricher
        {
            Name = nameof(TestEnricherA),
            IsActive = true
        });

        // Reset all photos to have no enrichers
        var photos = await photoRepo.GetAll().ToListAsync();
        foreach (var photo in photos)
        {
            photo.EnrichedWithEnricherType = EnricherType.None;
            await photoRepo.UpdateAsync(photo);
        }

        var photoIds = photos.Select(p => p.Id).ToList();

        // Act
        var processedCount = await service.ReEnrichMissingBatchAsync(photoIds);

        // Assert
        processedCount.Should().Be(photoIds.Count);

        // Verify all photos were updated
        _context.ChangeTracker.Clear();
        var updatedPhotos = await photoRepo.GetAll().ToListAsync();
        foreach (var photo in updatedPhotos)
        {
            photo.EnrichedWithEnricherType.Should().HaveFlag(EnricherType.Metadata);
        }
    }

    [Test]
    public async Task ReEnrichPhotoAsync_WithDependentEnrichers_RunsDependenciesFirst()
    {
        // Arrange
        var service = _provider.GetRequiredService<IReEnrichmentService>();
        var photoRepo = _provider.GetRequiredService<IRepository<Photo>>();

        var photo = await photoRepo.GetAll().FirstAsync();
        photo.EnrichedWithEnricherType = EnricherType.None;
        await photoRepo.UpdateAsync(photo);

        // Act - Request only TestEnricherB, which depends on TestEnricherA
        var enricherTypes = new[] { typeof(TestEnricherB) };
        var result = await service.ReEnrichPhotoAsync(photo.Id, enricherTypes);

        // Assert
        result.Should().BeTrue();

        // Verify both enrichers were run (including dependency)
        _context.ChangeTracker.Clear();
        var updatedPhoto = await photoRepo.GetAsync(photo.Id);
        updatedPhoto.EnrichedWithEnricherType.Should().HaveFlag(EnricherType.Metadata); // TestEnricherA
        updatedPhoto.EnrichedWithEnricherType.Should().HaveFlag(EnricherType.Tag); // TestEnricherB
    }

    private async Task SeedTestDataAsync()
    {
        var storage = new Storage
        {
            Id = 1,
            Name = "Test Storage",
            Folder = _testStorageFolder
        };

        _context.Storages.Add(storage);
        await _context.SaveChangesAsync();

        for (int i = 1; i <= 3; i++)
        {
            var photo = new Photo
            {
                Id = i,
                Name = $"test{i}",
                RelativePath = "photos",
                StorageId = storage.Id,
                Storage = storage,
                EnrichedWithEnricherType = EnricherType.None
            };

            _context.Photos.Add(photo);

            var file = new DbContext.Models.File
            {
                Name = "test.jpg",
                Photo = photo
            };

            _context.Files.Add(file);
        }

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }
}

// Test enrichers for integration testing
public class TestEnricherA : Services.Enrichers.IEnricher
{
    public EnricherType EnricherType => EnricherType.Metadata;
    public Type[] Dependencies => Array.Empty<Type>();

    public Task EnrichAsync(Photo photo, Services.Models.SourceDataDto source, System.Threading.CancellationToken cancellationToken = default)
    {
        // Simulate enrichment
        return Task.CompletedTask;
    }
}

public class TestEnricherB : Services.Enrichers.IEnricher
{
    public EnricherType EnricherType => EnricherType.Tag;
    public Type[] Dependencies => new[] { typeof(TestEnricherA) };

    public Task EnrichAsync(Photo photo, Services.Models.SourceDataDto source, System.Threading.CancellationToken cancellationToken = default)
    {
        // Simulate enrichment
        return Task.CompletedTask;
    }
}
