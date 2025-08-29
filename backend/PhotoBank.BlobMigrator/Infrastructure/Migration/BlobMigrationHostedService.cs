using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using PhotoBank.BlobMigrator;
using PhotoBank.DbContext.DbContext;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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

        await MigratePhotosAsync(ct);
//        await MigrateFacesAsync(ct);

        _log.LogInformation("Migration finished.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    // ============================= Photos =============================
    private async Task MigratePhotosAsync(CancellationToken ct)
    {
        _log.LogInformation("Photos migration started...");
        var sw = Stopwatch.StartNew();
        int migrated = 0, skipped = 0, failed = 0;

        List<int> ids;
        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            ids = await db.Database.SqlQueryRaw<int>(
                """
                SELECT TOP({0}) p.Id
                FROM Photos p WITH (NOLOCK)
                WHERE (p.S3Key_Preview IS NULL OR p.S3Key_Preview = '')
                ORDER BY p.Id
                """, _opt.BatchSize
            ).ToListAsync(ct);
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
        var needPreview = await db.Database.SqlQueryRaw<int>(
            """
            SELECT CAST(CASE WHEN p.PreviewImage IS NOT NULL AND (p.S3Key_Preview IS NULL OR p.S3Key_Preview='') 
                             THEN 1 ELSE 0 END AS int) AS Value
            FROM Photos p WITH (NOLOCK) 
            WHERE p.Id = {0}
            """, id
        ).FirstOrDefaultAsync(ct) == 1;

        var needThumb = await db.Database.SqlQueryRaw<int>(
            """
            SELECT CAST(CASE WHEN p.Thumbnail IS NOT NULL AND (p.S3Key_Thumbnail IS NULL OR p.S3Key_Thumbnail='') 
                             THEN 1 ELSE 0 END AS int) AS Value
            FROM Photos p WITH (NOLOCK)
            WHERE p.Id = {0}
            """, id
        ).FirstOrDefaultAsync(ct) == 1;

        if (!needPreview && !needThumb) return (false, true);

        var connStr = _cfg.GetConnectionString("DefaultConnection")!;

        // ---- PREVIEW (JPEG, кладём как есть) ----
        if (needPreview)
        {
            var tmp = Path.Combine(_opt.TempDir, $"photo_{id}_preview.jpg");
            await DumpBlobToFileAsync(connStr, "Photos", "PreviewImage", "Id", id, tmp, ct);
            try
            {
                var info = await LoadPhotoPathInfoAsync(db, id, ct);
                var key = BuildPreviewKey(info.Storage, info.RelativePath, id);

                var meta = await UploadFileAndGetMetaAsync(tmp, key, ct);

                await db.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE Photos
                    SET S3Key_Preview       = {0},
                        S3ETag_Preview      = {1},
                        Sha256_Preview      = {2},
                        BlobSize_Preview    = {3},
                        MigratedAt_Preview  = {4}
                    WHERE Id = {5}
                    """,
                    new object[] { key, meta.ETag, meta.Sha256Hex, meta.SizeBytes, DateTime.UtcNow, id }, ct);
            }
            finally { SafeDelete(tmp); }
        }

        // ---- THUMBNAIL (JPEG, кладём как есть) ----
        if (needThumb)
        {
            var tmp = Path.Combine(_opt.TempDir, $"photo_{id}_thumb.jpg");
            await DumpBlobToFileAsync(connStr, "Photos", "Thumbnail", "Id", id, tmp, ct);
            try
            {
                var info = await LoadPhotoPathInfoAsync(db, id, ct);
                var key = BuildThumbnailKey(info.Storage, info.RelativePath, id);

                var meta = await UploadFileAndGetMetaAsync(tmp, key, ct);

                await db.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE Photos
                    SET S3Key_Thumbnail       = {0},
                        S3ETag_Thumbnail      = {1},
                        Sha256_Thumbnail      = {2},
                        BlobSize_Thumbnail    = {3},
                        MigratedAt_Thumbnail  = {4}
                    WHERE Id = {5}
                    """,
                    new object[] { key, meta.ETag, meta.Sha256Hex, meta.SizeBytes, DateTime.UtcNow, id }, ct);
            }
            finally { SafeDelete(tmp); }
        }

        return (true, false);
    }

    // ============================= Faces =============================
    private async Task MigrateFacesAsync(CancellationToken ct)
    {
        _log.LogInformation("Faces migration started...");
        var sw = Stopwatch.StartNew();
        int migrated = 0, skipped = 0, failed = 0;

        List<int> ids;
        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            ids = await db.Database.SqlQueryRaw<int>(
                """
                SELECT TOP({0}) f.Id
                FROM Faces f WITH (NOLOCK)
                WHERE f.Image IS NOT NULL AND (f.S3Key_Image IS NULL OR f.S3Key_Image = '')
                ORDER BY f.Id
                """, _opt.BatchSize
            ).ToListAsync(ct);
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
            SELECT CAST(CASE WHEN f.Image IS NOT NULL AND (f.S3Key_Image IS NULL OR f.S3Key_Image='') 
                             THEN 1 ELSE 0 END AS int) AS Value
            FROM Faces f WITH (NOLOCK)
            WHERE f.Id = {0}
            """, id
        ).FirstOrDefaultAsync(ct) == 1;
        if (!need) return (false, true);

        var connStr = _cfg.GetConnectionString("DefaultConnection")!;
        var tmp = Path.Combine(_opt.TempDir, $"face_{id}.jpg");
        await DumpBlobToFileAsync(connStr, "Faces", "Image", "Id", id, tmp, ct);
        try
        {
            var key = BuildFaceKey(id);
            var meta = await UploadFileAndGetMetaAsync(tmp, key, ct);

            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE Faces
                SET S3Key_Image      = {0},
                    S3ETag_Image     = {1},
                    Sha256_Image     = {2},
                    BlobSize_Image   = {3},
                    MigratedAt_Image = {4}
                WHERE Id = {5}
                """,
                new object[] { key, meta.ETag, meta.Sha256Hex, meta.SizeBytes, DateTime.UtcNow, id }, ct);
        }
        finally
        {
            SafeDelete(tmp);
        }

        return (true, false);
    }

    // ============================= Helpers =============================
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

    // ---- S3 key builders (JPEG everywhere) ----
    private static string BuildFaceKey(long id)
        => $"faces/{id:0000000000}.jpg";

    private static string BuildPreviewKey(string storageNameOrCode, string? relativePath, long id)
        => BuildPhotoScopedKey("preview", storageNameOrCode, relativePath, $"{id:0000000000}_preview.jpg");

    private static string BuildThumbnailKey(string storageNameOrCode, string? relativePath, long id)
        => BuildPhotoScopedKey("thumbnail", storageNameOrCode, relativePath, $"{id:0000000000}_thumbnail.jpg");

    private static string BuildPhotoScopedKey(string scope, string storageNameOrCode, string? relativePath, string fileName)
    {
        var storage = SlugifySegment(storageNameOrCode);
        var rel = (relativePath ?? string.Empty).Replace('\\', '/');

        var segments = rel.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(SlugifySegment)
                          .Where(s => s.Length > 0)
                          .ToList();

        var basePrefix = new StringBuilder(scope).Append('/').Append(storage);
        if (segments.Count > 0) basePrefix.Append('/').Append(string.Join('/', segments));

        var key = $"{basePrefix}/{fileName}";

        // защита от слишком длинных ключей
        if (Encoding.UTF8.GetByteCount(key) > 1024)
        {
            var hash8 = ShortHash(key);
            var shortened = ShortenPath(basePrefix.ToString(), fileName, 1024 - (1 + hash8.Length));
            key = $"{shortened}-{hash8}";
        }
        return key;
    }

    private static string SlugifySegment(string value)
    {
        var src = value.Trim().Normalize(NormalizationForm.FormKC);
        src = new string(src.Where(ch => !char.IsControl(ch)).ToArray());
        src = Regex.Replace(src, @"[^\p{L}\p{Nd}\-_.]+", "-");
        src = Regex.Replace(src, @"-+", "-").Trim('-');
        if (src.Length > 100) src = src[..100];
        return src.Length == 0 ? "_" : src;
    }

    private static string ShortHash(string s)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes, 0, 4).ToLowerInvariant(); // 8 hex chars
    }

    private static string ShortenPath(string prefix, string fileName, int limitBytes)
    {
        var segs = prefix.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (segs.Count == 0) return prefix;

        for (int i = 1; i < segs.Count; i++)
        {
            while (Encoding.UTF8.GetByteCount(string.Join('/', segs) + "/" + fileName) > limitBytes && segs[i].Length > 6)
            {
                segs[i] = segs[i][..Math.Max(3, segs[i].Length - 1)];
            }
            if (Encoding.UTF8.GetByteCount(string.Join('/', segs) + "/" + fileName) <= limitBytes) break;
        }
        return string.Join('/', segs);
    }

    // ---- Upload helper (always JPEG) ----
    private readonly record struct UploadMeta(string ETag, string Sha256Hex, long SizeBytes);

    private async Task<UploadMeta> UploadFileAndGetMetaAsync(
        string filePath,
        string objectKey,
        CancellationToken ct)
    {
        var size = new FileInfo(filePath).Length;

        string sha256Hex;
        await using (var stream = File.OpenRead(filePath))
        using (var sha = SHA256.Create())
        {
            var hash = await sha.ComputeHashAsync(stream, ct);
            sha256Hex = Convert.ToHexString(hash);
        }

        var put = new PutObjectArgs()
            .WithBucket(_s3.Bucket)
            .WithObject(objectKey)
            .WithFileName(filePath)
            .WithContentType("image/jpeg"); // всегда JPEG

        await _minio.PutObjectAsync(put, ct);
        _log.LogDebug("Uploaded: {Key}", objectKey);

        var stat = await _minio.StatObjectAsync(
            new StatObjectArgs().WithBucket(_s3.Bucket).WithObject(objectKey), ct);

        var etag = stat.ETag ?? string.Empty;
        return new UploadMeta(etag, sha256Hex, size);
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

        var sql = $"SELECT {column} FROM {table} WITH (NOLOCK) WHERE {keyColumn} = @id";
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.BigInt) { Value = keyValue });

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
        if (!await reader.ReadAsync(ct))
            throw new InvalidOperationException($"Row not found: {table}.{keyColumn}={keyValue}");

        const int bufSize = 128 * 1024;
        var buffer = new byte[bufSize];
        long fieldOffset = 0;

        await using var fs = File.Create(filePath);
        long bytesRead;
        while ((bytesRead = reader.GetBytes(0, fieldOffset, buffer, 0, buffer.Length)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, (int)bytesRead), ct);
            fieldOffset += bytesRead;
        }
    }

    // ---- DB helpers ----
    private sealed record PhotoPathInfo(string Storage, string? RelativePath);

    private static async Task<PhotoPathInfo> LoadPhotoPathInfoAsync(PhotoBankDbContext db, long photoId, CancellationToken ct)
    {
        // Подстройте под вашу схему: Photos.StorageId -> Storages(Code)
        var sql = """
                  SELECT TOP(1) s.Name AS Storage, p.RelativePath
                  FROM Photos p WITH (NOLOCK)
                  JOIN Storages s WITH (NOLOCK) ON s.Id = p.StorageId
                  WHERE p.Id = {0}
                  """;
        var row = await db.Database.SqlQueryRaw<PhotoPathInfo>(sql, photoId).FirstOrDefaultAsync(ct);
        if (row is null) throw new InvalidOperationException($"Photo {photoId} not found or storage missing");
        return row;
    }
}
