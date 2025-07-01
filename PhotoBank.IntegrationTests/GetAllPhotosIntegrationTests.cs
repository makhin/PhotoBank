using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class GetAllPhotosIntegrationTests
{
    private ServiceProvider _provider = null!;
    private IConfiguration _config = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _config = new ConfigurationBuilder()
            .SetBasePath(TestContext.CurrentContext.TestDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
    }

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        var connectionString = _config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found");
        services.AddDbContext<PhotoBankDbContext>(options =>
            options.UseSqlServer(connectionString,
                builder =>
                {
                    builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                    builder.UseNetTopologySuite();
                    builder.CommandTimeout(120);
                }));
        RegisterServicesForApi.Configure(services);
        services.AddAutoMapper(typeof(MappingProfile));
        _provider = services.BuildServiceProvider();
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
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(5180);
        result.Photos.Should().HaveCount(5180);
    }

    [Test]
    public async Task GetAllPhotosAsync_NoFilterTop10_Returns10()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Top = 10
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(5180);
        result.Photos.Should().HaveCount(10);
    }

    [Test]
    public async Task GetAllPhotosAsync_NoFilterTopAndSkip10_Returns10()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Top = 10,
            Skip = 10
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(5180);
        result.Photos.Should().HaveCount(10);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByIsBW_ReturnsOnlyBW()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            IsBW = true
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(60);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByIsAdult_ReturnsOnlyAdult()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            IsAdultContent = true
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(20);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByIsRacy_ReturnsOnlyRacy()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            IsRacyContent = true
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(71);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByStorage_ReturnsMatchingPhoto()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Storages = new []{3, 4}
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(1385);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByStorageAndPath_ReturnsMatchingPhoto()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Storages = new[] { 3, 4 },
            RelativePath = "Test"
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(0);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByTag_ReturnsMatchingPhoto()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Tags = new []{3504, 3505}
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(2420);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByPerson_ReturnsMatchingPhoto()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Persons = new[] { 1, 2 }
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(104);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByCaption_ReturnsMatchingPhoto()
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Caption = "sky and grass"
        };
        var result = await service.GetAllPhotosAsync(filterDto);
        result.Count.Should().Be(163);
    }
}
