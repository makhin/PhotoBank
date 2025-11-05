using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DependencyInjection;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using Minio;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class GetAllPhotosIntegrationTests
{
    private ServiceProvider _provider = null!;
    private IConfiguration _config = null!;
    private readonly DateTime _takenDateFrom = new DateTime(2015, 1, 1);
    private readonly DateTime _takenDateTo = new DateTime(2015, 12, 31);

    private async Task<PageResponse<PhotoItemDto>> MeasureGetAllPhotosAsync(FilterDto filter)
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
        // Configure Npgsql to treat DateTime with Kind=Unspecified as UTC
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        _config = new ConfigurationBuilder()
            .SetBasePath(TestContext.CurrentContext.TestDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.example.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "issuer",
                ["Jwt:Audience"] = "audience",
                ["Jwt:Key"] = "secret"
            })
            .Build();
    }

    [SetUp]
    public void Setup()
    {
        try
        {
            var services = new ServiceCollection();
            var connectionString = _config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found");
            services.AddDbContext<PhotoBankDbContext>(options =>
                options.UseNpgsql(connectionString,
                    builder =>
                    {
                        builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                        builder.UseNetTopologySuite();
                        builder.CommandTimeout(120);
                    }));
            services.AddDbContext<AccessControlDbContext>(options =>
                options.UseNpgsql(connectionString));
            services.AddPhotobankCore(_config);
            services.AddPhotobankApi(_config);
            services.AddPhotobankCors();

            services.RemoveAll<ICurrentUserAccessor>();
            services.AddScoped<ICurrentUserAccessor>(_ => new TestCurrentUserAccessor(new DummyCurrentUser()));

            services.AddLogging();
            services.AddSingleton<IMinioClient>(Mock.Of<IMinioClient>());
            _provider = services.BuildServiceProvider();
            var db = _provider.GetRequiredService<PhotoBankDbContext>();
            db.Database.OpenConnection();
        }
        catch (Exception ex)
        {
            Assert.Ignore("PostgreSQL not available: " + ex.Message);
        }
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
    }

    [Test]
    public async Task GetAllPhotosAsync_NoFilter_ReturnsLimitedSet()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
            PageSize = 10000 // Should be capped to MaxPageSize
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(5180);
        result.Items.Should().HaveCount(PageRequest.MaxPageSize);
    }

    [Test]
    public async Task GetAllPhotosAsync_NoFilterTop10_Returns10()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
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
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
            PageSize = 10,
            Page = 2
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.Items.Should().HaveCount(10);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterBySingleDay_ReturnsPhotosWithinDay()
    {
        var discoveryFilter = new FilterDto()
        {
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
            PageSize = PageRequest.MaxPageSize
        };

        var discoveryResult = await MeasureGetAllPhotosAsync(discoveryFilter);
        var samplePhoto = discoveryResult.Items
            .FirstOrDefault(p => p.TakenDate.HasValue && p.TakenDate.Value.TimeOfDay > TimeSpan.Zero);
        samplePhoto.Should().NotBeNull("the seeded dataset should contain a photo with a time component within the selected range");

        var targetDate = samplePhoto!.TakenDate!.Value.Date;

        var db = _provider.GetRequiredService<PhotoBankDbContext>();
        var expectedCount = await db.Photos.CountAsync(p => p.TakenDate.HasValue && p.TakenDate.Value.Date == targetDate);
        expectedCount.Should().BeGreaterThan(0);

        var singleDayFilter = new FilterDto()
        {
            TakenDateFrom = targetDate,
            TakenDateTo = targetDate,
            PageSize = PageRequest.MaxPageSize
        };

        var singleDayResult = await MeasureGetAllPhotosAsync(singleDayFilter);

        singleDayResult.TotalCount.Should().Be(expectedCount);
        singleDayResult.Items.Should().OnlyContain(p => p.TakenDate.HasValue && p.TakenDate.Value.Date == targetDate);
    }

    [Test]
    public async Task GetAllPhotosAsync_FilterByIsBW_ReturnsOnlyBW()
    {
        var filterDto = new FilterDto()
        {
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
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
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
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
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
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
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
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
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
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
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
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
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
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
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
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
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
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
            TakenDateFrom = _takenDateFrom,
            TakenDateTo = _takenDateTo,
            Caption = "sky and grass"
        };
        var result = await MeasureGetAllPhotosAsync(filterDto);
        result.TotalCount.Should().Be(4);
    }
    private sealed class TestCurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly ICurrentUser _user;

        public TestCurrentUserAccessor(ICurrentUser user)
        {
            _user = user;
        }

        public ValueTask<ICurrentUser> GetCurrentUserAsync(CancellationToken ct = default)
            => ValueTask.FromResult(_user);

        public ICurrentUser CurrentUser => _user;
    }
}

