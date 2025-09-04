using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Moq;
using NUnit.Framework;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DependencyInjection;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class GetPhotoIntegrationTests
{
    private ServiceProvider _provider = null!;
    private IConfiguration _config = null!;

    private async Task<PhotoDto> MeasureGetPhotoAsync(int id)
    {
        var service = _provider.GetRequiredService<IPhotoService>();
        var sw = Stopwatch.StartNew();
        var result = await service.GetPhotoAsync(id);
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
                options.UseSqlServer(connectionString,
                    builder =>
                    {
                        builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                        builder.UseNetTopologySuite();
                        builder.CommandTimeout(120);
                    }));
            services
                .AddPhotobankCore(_config)
                .AddScoped<ICurrentUser, DummyCurrentUser>()
                .AddPhotobankApi(_config)
                .AddPhotobankCors();

            services.AddLogging();
            services.AddSingleton<IMinioClient>(Mock.Of<IMinioClient>());
            _provider = services.BuildServiceProvider();
            var db = _provider.GetRequiredService<PhotoBankDbContext>();
            db.Database.OpenConnection();
        }
        catch (Exception ex)
        {
            Assert.Ignore("SQL Server not available: " + ex.Message);
        }
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
    }

    [TestCase(10000)]
    [TestCase(30000)]
    public async Task GetPhotoAsync_AdminUser_ReturnsPhotoDto(int testId)
    {
        // Act
        var result = await MeasureGetPhotoAsync(testId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(testId);
    }
}
