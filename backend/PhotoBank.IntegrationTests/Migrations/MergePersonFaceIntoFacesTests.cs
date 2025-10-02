using System;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;
using PhotoBank.DbContext.DbContext;

namespace PhotoBank.IntegrationTests.Migrations;

[TestFixture]
public class MergePersonFaceIntoFacesTests
{
    private MsSqlContainer _dbContainer = null!;
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
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
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_dbContainer != null)
        {
            await _dbContainer.DisposeAsync();
        }
    }

    [Test]
    public async Task MergePersonFaceIntoFaces_MigratesExistingLinks()
    {
        await using (var migrationContext = CreateContext())
        {
            var migrator = migrationContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("0");
            await migrator.MigrateAsync("20250903090409_AddMonthDayIndexes");
        }

        await SeedLegacyPersonFaceDataAsync();

        await using (var upgradeContext = CreateContext())
        {
            await upgradeContext.Database.MigrateAsync();
        }

        await using (var verificationContext = CreateContext())
        {
            var migratedFace = await verificationContext.Faces.AsNoTracking().SingleAsync(f => f.Id == 3000);

            migratedFace.PersonId.Should().Be(2000);
            migratedFace.Provider.Should().Be("Aws");
            migratedFace.ExternalId.Should().Be("ext-123");
            migratedFace.ExternalGuid.Should().Be(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var command = conn.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = 'PersonFace'";
        var remaining = (int)(await command.ExecuteScalarAsync());
        remaining.Should().Be(0);
    }

    private async Task SeedLegacyPersonFaceDataAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var builder = new StringBuilder();
        builder.AppendLine("SET IDENTITY_INSERT Storages ON;");
        builder.AppendLine("INSERT INTO Storages (Id, Name, Folder) VALUES (10, 'legacy-storage', 'faces');");
        builder.AppendLine("SET IDENTITY_INSERT Storages OFF;\n");

        builder.AppendLine("SET IDENTITY_INSERT Photos ON;");
        builder.AppendLine(@"INSERT INTO Photos (Id, AccentColor, AdultScore, BlobSize_Preview, BlobSize_Thumbnail, DominantColorBackground, DominantColorForeground, DominantColors, EnrichedWithEnricherType, FaceIdentifyStatus, Height, ImageHash, IsAdultContent, IsBW, IsRacyContent, Location, MigratedAt_Preview, MigratedAt_Thumbnail, Name, Orientation, RacyScore, RelativePath, S3ETag_Preview, S3ETag_Thumbnail, S3Key_Preview, S3Key_Thumbnail, Scale, Sha256_Preview, Sha256_Thumbnail, StorageId, TakenDate, Width)");
        builder.AppendLine(@"VALUES (1000, 'ffffff', 0, NULL, NULL, 'bg', 'fg', 'colors', 0, 0, NULL, 'image-hash', 0, 0, 0, geometry::STGeomFromText('POINT (0 0)', 0), NULL, NULL, 'legacy-photo', NULL, 0, 'relative-path', 'etag-preview', 'etag-thumb', 'key-preview', 'key-thumb', 1, 'sha-preview', 'sha-thumb', 10, NULL, NULL);");
        builder.AppendLine("SET IDENTITY_INSERT Photos OFF;\n");

        builder.AppendLine("SET IDENTITY_INSERT Persons ON;");
        builder.AppendLine("INSERT INTO Persons (Id, DateOfBirth, ExternalGuid, ExternalId, Name, Provider) VALUES (2000, NULL, '55555555-5555-5555-5555-555555555555', NULL, 'Legacy Person', NULL);");
        builder.AppendLine("SET IDENTITY_INSERT Persons OFF;\n");

        builder.AppendLine("SET IDENTITY_INSERT Faces ON;");
        builder.AppendLine(@"INSERT INTO Faces (Id, Age, BlobSize_Image, FaceAttributes, Gender, IdentifiedWithConfidence, IdentityStatus, MigratedAt_Image, PersonId, PhotoId, Rectangle, S3ETag_Image, S3Key_Image, Sha256_Image, Smile)");
        builder.AppendLine(@"VALUES (3000, NULL, NULL, '{}', NULL, 0, 0, NULL, NULL, 1000, geometry::STGeomFromText('POINT (0 0)', 0), 'face-etag', 'face-key', 'face-hash', NULL);");
        builder.AppendLine("SET IDENTITY_INSERT Faces OFF;\n");

        builder.AppendLine("INSERT INTO PersonFace (FaceId, PersonId, Provider, ExternalId, ExternalGuid) VALUES (3000, 2000, 'Aws', 'ext-123', '44444444-4444-4444-4444-444444444444');");

        using var command = conn.CreateCommand();
        command.CommandText = builder.ToString();
        await command.ExecuteNonQueryAsync();
    }

    private PhotoBankDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PhotoBankDbContext>()
            .UseSqlServer(_connectionString, builder =>
            {
                builder.MigrationsAssembly(typeof(PhotoBankDbContext).Assembly.GetName().Name);
                builder.UseNetTopologySuite();
            })
            .Options;

        return new PhotoBankDbContext(options);
    }
}
