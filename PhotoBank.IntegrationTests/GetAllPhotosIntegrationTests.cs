using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class GetAllPhotosIntegrationTests
{
    private ServiceProvider _provider = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddDbContext<PhotoBankDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        RegisterServicesForApi.Configure(services);
        services.AddAutoMapper(typeof(MappingProfile));
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        SeedData(context);
    }

    private static void SeedData(PhotoBankDbContext context)
    {
        var storage = new Storage { Name = "s", Folder = "f" };
        var tag1 = new Tag { Name = "tag1" };
        var tag2 = new Tag { Name = "tag2" };
        context.AddRange(storage, tag1, tag2);
        context.SaveChanges();

        var photo1 = new Photo
        {
            Name = "p1",
            StorageId = storage.Id,
            IsBW = true,
            Thumbnail = new byte[] {1},
            RelativePath = "r1",
            PhotoTags = new List<PhotoTag>{ new() { TagId = tag1.Id } }
        };
        var photo2 = new Photo
        {
            Name = "p2",
            StorageId = storage.Id,
            IsBW = false,
            Thumbnail = new byte[] {1},
            RelativePath = "r2",
            PhotoTags = new List<PhotoTag>{ new() { TagId = tag2.Id } }
        };
        context.AddRange(photo1, photo2);
        context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
    }

    [Test]
    public async Task GetAllPhotosAsync_NoFilter_ReturnsAll()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var result = await service.GetAllPhotosAsync(new FilterDto());
        result.Count.Should().Be(2);
        result.Photos.Should().HaveCount(2);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByIsBW_ReturnsOnlyBW()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var result = await service.GetAllPhotosAsync(new FilterDto { IsBW = true });
        result.Count.Should().Be(1);
        result.Photos!.Single().Name.Should().Be("p1");
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByTag_ReturnsMatchingPhoto()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var result = await service.GetAllPhotosAsync(new FilterDto { Tags = new[] {1} });
        result.Count.Should().Be(1);
        result.Photos!.Single().Name.Should().Be("p1");
    }
}
