using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.Exceptions;
using Minio.DataModel.Args;
using PhotoBank.DbContext.DbContext;

public sealed class BlobMigrationHostedService : IHostedService
{
    private readonly IDbContextFactory<PhotoBankDbContext> _dbFactory;
    private readonly IMinioClient _minio;
    private readonly BlobMigrationOptions _opt;
    private readonly S3Options _s3;
    private readonly ILogger<BlobMigrationHostedService> _log;
    private readonly IConfiguration _cfg;

    public BlobMigrationHostedService(
        IDbContextFactory<PhotoBankDbContext> dbFactory,
        IMinioClient minio,
        IOptions<BlobMigrationOptions> opt,
        IOptions<S3Options> s3,
        ILogger<BlobMigrationHostedService> log,
        IConfiguration cfg)
    {
        _dbFactory = dbFactory;
        _minio = minio;
        _opt = opt.Value;
        _s3 = s3.Value;
        _log = log;
        _cfg = cfg;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_opt.Enabled)
        {
            _log.LogInformation("Blob migration is disabled. Exit.");
            return;
        }

        Directory.CreateDirectory(_opt.TempDir);

        await EnsureBucketAsync(ct);

        // Примеры двух прогонов — подстройте под свои реальные критерии выборки:
        await MigratePhotosAsync(ct);
        await MigrateFacesAsync(ct);

        _log.LogInformation("Migration finished.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    // ---------------- Migration: Photos ----------------
    private async Task MigratePhotosAsync(CancellationToken ct)
    {
        _log.LogInformation("Photos migration started...");
        var sw = Stopwatch.StartNew();
        int migrated = 0, skipped = 0, failed = 0;

        // Берем пачку id по условию (пример: где есть превью/thumbnail, но нет ключей в S3)
        List<long> ids;
        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            ids = await db.Database.SqlQueryRaw<long>(
                """
                SELECT TOP({0}) p.Id
                FROM Photos p WITH (NOLOCK)
                WHERE (p.Preview IS NOT NULL AND (p.S3Key_Preview IS NULL OR p.S3Key_Preview = ''))
                   OR (p.Thumbnail IS NOT NULL AND (p.S3Key_Thumbnail IS NULL OR p.S3Key_Thumbnail = ''))
                ORDER BY p.Id
                """, _opt.BatchSize
            ).AsNoTracking().ToListAsync(ct);
        }

        _log.LogInformation("Photos to process: {Count}", ids.Count);

        using var throttler = new SemaphoreSlim(_opt.Concurrency);
        var tasks = ids.Select(async id =>
        {
            await throttler.WaitAsync(ct);
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var (ok, wasSkipped) = await MigratePhotoRowAsync(db, id, ct);
                if (ok) Interlocked.Increment(ref migrated);
                else if (wasSkipped) Interlocked.Increment(ref skipped);
                else Interlocked.Increment(ref failed);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Photos:{Id} failed", id);
                Interlocked.Increment(ref failed);
            }
            finally
            {
                throttler.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        sw.Stop();
        _log.LogInformation("Photos: migrated={Migrated}, skipped={Skipped}, failed={Failed} in {Elapsed}",
            migrated, skipped, failed, sw.Elapsed);
    }

    private async Task<(bool ok, bool skipped)> MigratePhotoRowAsync(PhotoBankDbContext db, long id, CancellationToken ct)
    {
        // Выясняем, что конкретно надо мигрировать
        var needPreview = await db.Database.SqlQueryRaw<int>(
            """
            SELECT CASE WHEN p.Preview IS NOT NULL AND (p.S3Key_Preview IS NULL OR p.S3Key_Preview='') THEN 1 ELSE 0 END
            FROM Photos p WITH (NOLOCK) WHERE p.Id = {0}
            """, id).AsNoTracking().FirstOrDefaultAsync(ct) == 1;

        var needThumb = await db.Database.SqlQueryRaw<int>(
            """
            SELECT CASE WHEN p.Thumbnail IS NOT NULL AND (p.S3Key_Thumbnail IS NULL OR p.S3Key_Thumbnail='') THEN 1 ELSE 0 END
            FROM Photos p WITH (NOLOCK) WHERE p.Id = {0}
            """, id).AsNoTracking().FirstOrDefaultAsync(ct) == 1;

        if (!needPreview && !needThumb) return (false, true);

        var connStr = _cfg.GetConnectionString("DefaultConnection")!;
        if (needPreview)
        {
            var tmp = Path.Combine(_opt.TempDir, $"photo_{id}_preview.bin");
            await DumpBlobToFileAsync(connStr, "Photos", "Preview", "Id", id, tmp, ct);
            try
            {
                using var image = new MagickImage(tmp);
                image.Strip();                 // убрать метаданные
                image.Quality = 82;            // компромисс качество/размер
                image.Settings.Interlace = Interlace.Jpeg; // прогрессивный JPEG
                image.Format = MagickFormat.Jpg;
                image.Write(tmp);

                var key = BuildKey("photos", id, "preview");
                await UploadToS3Async(tmp, key, ct);
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE Photos SET S3Key_Preview = {0} WHERE Id = {1}", [key, id], ct);
            }
            finally
            {
                SafeDelete(tmp);
            }
        }

        if (needThumb)
        {
            var tmp = Path.Combine(_opt.TempDir, $"photo_{id}_thumb.bin");
            await DumpBlobToFileAsync(connStr, "Photos", "Thumbnail", "Id", id, tmp, ct);
            try
            {
                var key = BuildKey("photos", id, "thumbnail");
                await UploadToS3Async(tmp, key, ct);
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE Photos SET S3Key_Thumbnail = {0} WHERE Id = {1}", [key, id], ct);
            }
            finally
            {
                SafeDelete(tmp);
            }
        }

        return (true, false);
    }

    // ---------------- Migration: Faces ----------------
    private async Task MigrateFacesAsync(CancellationToken ct)
    {
        _log.LogInformation("Faces migration started...");
        var sw = Stopwatch.StartNew();
        int migrated = 0, skipped = 0, failed = 0;

        List<long> ids;
        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            ids = await db.Database.SqlQueryRaw<long>(
                """
                SELECT TOP({0}) f.Id
                FROM Faces f WITH (NOLOCK)
                WHERE f.Image IS NOT NULL AND (f.S3Key_Image IS NULL OR f.S3Key_Image = '')
                ORDER BY f.Id
                """, _opt.BatchSize
            ).AsNoTracking().ToListAsync(ct);
        }

        _log.LogInformation("Faces to process: {Count}", ids.Count);

        using var throttler = new SemaphoreSlim(_opt.Concurrency);
        var tasks = ids.Select(async id =>
        {
            await throttler.WaitAsync(ct);
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var (ok, wasSkipped) = await MigrateFaceRowAsync(db, id, ct);
                if (ok) Interlocked.Increment(ref migrated);
                else if (wasSkipped) Interlocked.Increment(ref skipped);
                else Interlocked.Increment(ref failed);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Faces:{Id} failed", id);
                Interlocked.Increment(ref failed);
            }
            finally
            {
                throttler.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        sw.Stop();
        _log.LogInformation("Faces: migrated={Migrated}, skipped={Skipped}, failed={Failed} in {Elapsed}",
            migrated, skipped, failed, sw.Elapsed);
    }

    private async Task<(bool ok, bool skipped)> MigrateFaceRowAsync(PhotoBankDbContext db, long id, CancellationToken ct)
    {
        var need = await db.Database.SqlQueryRaw<int>(
            """
            SELECT CASE WHEN f.Image IS NOT NULL AND (f.S3Key_Image IS NULL OR f.S3Key_Image='') THEN 1 ELSE 0 END
            FROM Faces f WITH (NOLOCK) WHERE f.Id = {0}
            """, id).AsNoTracking().FirstOrDefaultAsync(ct) == 1;

        if (!need) return (false, true);

        var connStr = _cfg.GetConnectionString("DefaultConnection")!;

        var tmp = Path.Combine(_opt.TempDir, $"face_{id}.bin");
        await DumpBlobToFileAsync(connStr, "Faces", "Image", "Id", id, tmp, ct);
        try
        {
            var key = BuildKey("faces", id, "image");
            await UploadToS3Async(tmp, key, ct);
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE Faces SET S3Key_Image = {0} WHERE Id = {1}", [key, id], ct);
        }
        finally
        {
            SafeDelete(tmp);
        }

        return (true, false);
    }

    // ---------------- Helpers ----------------

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        try
        {
            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_s3.Bucket), ct);
            if (!exists)
            {
                await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_s3.Bucket), ct);
                _log.LogInformation("Bucket '{Bucket}' created.", _s3.Bucket);
            }
        }
        catch (MinioException ex)
        {
            _log.LogError(ex, "EnsureBucket failed");
            throw;
        }
    }

    private static string BuildKey(string scope, long id, string alias)
        => $"{scope}/{id:0000000000}/{alias}.bin";

    private async Task UploadToS3Async(string filePath, string objectKey, CancellationToken ct)
    {
        var put = new PutObjectArgs()
            .WithBucket(_s3.Bucket)
            .WithObject(objectKey)
            .WithFileName(filePath)
            .WithContentType("application/octet-stream");
        await _minio.PutObjectAsync(put, ct);
        _log.LogDebug("Uploaded: {Key}", objectKey);
    }

    private static void SafeDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* suppress */ }
    }

    /// <summary>
    /// Потоково выгружает BLOB в файл (SequentialAccess), без загрузки всего в память.
    /// </summary>
    private static async Task DumpBlobToFileAsync(
        string connectionString,
        string table,
        string column,
        string keyColumn,
        long keyValue,
        string filePath,
        CancellationToken ct)
    {
        await using var con = new SqlConnection(connectionString);
        await con.OpenAsync(ct);

        // Важно: SequentialAccess + GetBytes в цикле; WITH (NOLOCK) приемлемо для "неизменяемых" blob'ов
        var sql = $"SELECT {column} FROM {table} WITH (NOLOCK) WHERE {keyColumn} = @id";
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.BigInt) { Value = keyValue });

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);

        if (!await reader.ReadAsync(ct))
            throw new InvalidOperationException($"Row not found: {table}.{keyColumn}={keyValue}");

        const int bufSize = 128 * 1024;
        var buffer = new byte[bufSize];
        long bytesRead;
        long fieldOffset = 0;

        await using var fs = File.Create(filePath);
        while ((bytesRead = reader.GetBytes(0, fieldOffset, buffer, 0, buffer.Length)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, (int)bytesRead), ct);
            fieldOffset += bytesRead;
        }
    }
}