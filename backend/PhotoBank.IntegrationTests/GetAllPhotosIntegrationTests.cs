using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class GetAllPhotosIntegrationTests
{
    private ServiceProvider _provider = null!;
    private IConfiguration _config = null!;

    private async Task<QueryResult> MeasureGetAllPhotosAsync(FilterDto filter)
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var sw = Stopwatch.StartNew();
        var result = await service.GetAllPhotosAsync(filter);
        sw.Stop();
        TestContext.WriteLine($"{TestContext.CurrentContext.Test.Name}: {sw.ElapsedMilliseconds} ms");
        return result;
    }

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

        services.AddLogging();
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
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
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            PageSize = 10000
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(5180);
        result.Items.Should().HaveCount(5180);
    }

    [Test]
    public async Task GetAllPhotosAsync_NoFilterTop10_Returns10()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            PageSize = 10
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(5180);
        result.Items.Should().HaveCount(10);
    }

    [Test]
    public async Task GetAllPhotosAsync_NoFilterTopAndSkip10_Returns10()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            PageSize = 10,
            Page = 2
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(5180);
        result.Items.Should().HaveCount(10);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByIsBW_ReturnsOnlyBW()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            IsBW = true
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(60);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByIsAdult_ReturnsOnlyAdult()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            IsAdultContent = true
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(20);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByIsRacy_ReturnsOnlyRacy()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            IsRacyContent = true
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(71);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByStorage_ReturnsMatchingPhoto()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Storages = new []{3, 4}
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(1385);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByStorageAndPath_ReturnsMatchingPhoto()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Storages = new[] { 3, 4 },
            RelativePath = "Test"
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByOneTag_ReturnsMatchingPhoto()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Tags = new []{ 3504 }
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(2462);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByOnePerson_ReturnsMatchingPhoto()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Persons = new[] { 1 }
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(401);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByTwoTags_ReturnsMatchingPhoto()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Tags = new[] { 3504, 3505 }
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(2420);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByTwoPersons_ReturnsMatchingPhoto()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Persons = new[] { 1, 2 }
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(104);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByCaption_ReturnsMatchingPhoto()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = new DateTime(2015, 1, 1),
            TakenDateTo = new DateTime(2016, 1, 1),
            Caption = "sky and grass"
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(163);
    }
}
