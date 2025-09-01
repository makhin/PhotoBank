using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Minio.DataModel.Args;
using Moq;
using NUnit.Framework;
using PhotoBank.Api.Controllers;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.DependencyInjection;
using PhotoBank.Services.Api;
using Respawn;

namespace PhotoBank.IntegrationTests;

[TestFixture]
public class FaceImageEndpointTests
{
    private MsSqlContainer _dbContainer = null!;
    private Respawner _respawner = null!;
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        try
        {
            _dbContainer = new MsSqlBuilder().WithPassword("yourStrong(!)Password").Build();
            await _dbContainer.StartAsync();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Docker endpoint"))
        {
            Assert.Ignore("Docker not available: " + ex.Message);
        }
        _connectionString = _dbContainer.GetConnectionString();

        var services = new ServiceCollection();
        services.AddDbContext<PhotoBankDbContext>(options =>
            options.UseSqlServer(_connectionString, builder =>
            {
                builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                builder.UseNetTopologySuite();
            }));
        services
            .AddPhotobankCore()
            .AddScoped<ICurrentUser, DummyCurrentUser>()
            .AddPhotobankApi();
        services.AddLogging();
        services.AddSingleton<IMinioClient>(Mock.Of<IMinioClient>());
        await using var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<PhotoBankDbContext>();
        await db.Database.MigrateAsync();

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer
        });
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_dbContainer != null)
            await _dbContainer.DisposeAsync();
    }

    [SetUp]
    public async Task Setup()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    private ServiceProvider BuildProvider(IMinioClient minioClient)
    {
        var services = new ServiceCollection();
        services.AddDbContext<PhotoBankDbContext>(options =>
            options.UseSqlServer(_connectionString, builder =>
            {
                builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                builder.UseNetTopologySuite();
            }));
        services
            .AddPhotobankCore()
            .AddScoped<ICurrentUser, DummyCurrentUser>()
            .AddPhotobankApi();
        services.AddLogging();
        services.AddSingleton(minioClient);
        return services.BuildServiceProvider();
    }

    private static void SeedFace(PhotoBankDbContext db, string s3Key, string eTag)
    {
        var storage = new Storage { Name = "s", Folder = "f" };
        db.Storages.Add(storage);
        var photo = new Photo { Name = "p", Storage = storage };
        db.Photos.Add(photo);
        db.Faces.Add(new Face { Photo = photo, S3Key_Image = s3Key, S3ETag_Image = eTag });
        db.SaveChanges();
    }

    [Test]
    public async Task GetImage_ReturnsPresignedUrl_WhenAvailable()
    {
        var url = "http://minio/face-key";
        var minio = new Mock<IMinioClient>();
        minio.Setup(m => m.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
            .ReturnsAsync(url);
        await using var provider = BuildProvider(minio.Object);
        var db = provider.GetRequiredService<PhotoBankDbContext>();
        SeedFace(db, "face-key", "etag");
        var controller = new FacesController(provider.GetRequiredService<IPhotoService>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var faceId = db.Faces.Single().Id;
        var result = await controller.GetImage(faceId);

        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(StatusCodes.Status301MovedPermanently);
        controller.Response.Headers.Location.ToString().Should().Be(url);
        controller.Response.Headers.ETag.ToString().Should().Be("\"etag\"");
    }

    [Test]
    public async Task GetImage_StreamsBytes_WhenPresignFails()
    {
        var data = new byte[] { 1, 2, 3 };
        var minio = new Mock<IMinioClient>();
        minio.Setup(m => m.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
            .ThrowsAsync(new Exception("fail"));
        minio.Setup(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns<GetObjectArgs, CancellationToken>((args, ct) =>
            {
                var prop = typeof(GetObjectArgs).GetProperty("CallBack", BindingFlags.NonPublic | BindingFlags.Instance);
                var cb = (Func<Stream, CancellationToken, Task>)prop!.GetValue(args)!;
                return cb(new MemoryStream(data), ct).ContinueWith(_ => (Minio.DataModel.ObjectStat)null!);
            });

        await using var provider = BuildProvider(minio.Object);
        var db = provider.GetRequiredService<PhotoBankDbContext>();
        SeedFace(db, "face-key", "etag");
        var controller = new FacesController(provider.GetRequiredService<IPhotoService>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var faceId = db.Faces.Single().Id;
        var result = await controller.GetImage(faceId);

        var file = result as FileContentResult;
        file.Should().NotBeNull();
        file!.FileContents.Should().Equal(data);
        controller.Response.Headers.ETag.ToString().Should().Be("\"etag\"");
    }
}

