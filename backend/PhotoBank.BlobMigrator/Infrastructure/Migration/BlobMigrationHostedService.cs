using System.Buffers;
using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotoBank.DbContext.DbContext;

public sealed class BlobMigrationOptions
{
    public bool Enabled { get; set; } = false;
    public int BatchSize { get; set; } = 200;
    public int Concurrency { get; set; } = 3;
    public string TempDir { get; set; } = "tmp-migrate";
}

public sealed class S3Options
{
    public string Endpoint { get; set; } = default!;
    public bool UseSsl { get; set; }
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string Bucket { get; set; } = default!;
}

public class BlobMigrationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMinioClient _minio;
    private readonly BlobMigrationOptions _opt;
    private readonly S3Options _s3;

    public BlobMigrationHostedService(
        IServiceScopeFactory scopeFactory,
        IMinioClient minio,
        IOptions<BlobMigrationOptions> opt,
        IOptions<S3Options> s3)
    {
        _scopeFactory = scopeFactory;
        _minio = minio;
        _opt = opt.Value;
        _s3 = s3.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_opt.Enabled) return;

        Directory.CreateDirectory(_opt.TempDir);
        await EnsureBucket(ct);

        // Стратегия: Photos (Preview, Thumbnail), затем Faces (Image)
        await MigratePhotos(ct);
        await MigrateFaces(ct);
    }

    private async Task EnsureBucket(CancellationToken ct)
    {
        var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_s3.Bucket), ct);
        if (!exists)
            await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_s3.Bucket), ct);
    }

    private async Task MigratePhotos(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();

            var ids = await db.Photos.AsNoTracking()
                .Where(p => p.S3Key_Preview == null || p.S3Key_Thumbnail == null)
                .OrderBy(p => p.Id)
                .Select(p => p.Id)
                .Take(_opt.BatchSize)
                .ToListAsync(ct);

            if (ids.Count == 0) break;

            using var throttler = new SemaphoreSlim(_opt.Concurrency);
            var tasks = ids.Select(async id =>
            {
                await throttler.WaitAsync(ct);
                try { await MigratePhotoRow(db, id, ct); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Photos:{id}] {ex.Message}");
                }
                finally { throttler.Release(); }
            }).ToArray();

            await Task.WhenAll(tasks);
        }
    }

    private async Task MigrateFaces(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PhotoBankDbContext>();

            var ids = await db.Faces.AsNoTracking()
                .Where(f => f.S3Key_Image == null)
                .OrderBy(f => f.Id)
                .Select(f => f.Id)
                .Take(_opt.BatchSize)
                .ToListAsync(ct);

            if (ids.Count == 0) break;

            using var throttler = new SemaphoreSlim(_opt.Concurrency);
            var tasks = ids.Select(async id =>
            {
                await throttler.WaitAsync(ct);
                try { await MigrateFaceRow(db, id, ct); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Faces:{id}] {ex.Message}");
                }
                finally { throttler.Release(); }
            }).ToArray();

            await Task.WhenAll(tasks);
        }
    }

    private static string ChooseContentType(string alias) =>
        alias switch
        {
            "preview" => "image/jpeg",
            "thumbnail" => "image/jpeg",
            "face" => "image/jpeg",
            _ => "application/octet-stream"
        };

    private string BuildKey(string prefix, int id, string? ext = ".jpg")
        => $"{prefix}/{DateTime.UtcNow:yyyy/MM/dd}/{id}{ext}".Replace("//", "/");

    // -------- Photos row --------
    private async Task MigratePhotoRow(PhotoBankDbContext db, int id, CancellationToken ct)
    {
        // Берём имя файла (если есть)
        var name = await db.Photos.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => p.Name) // если у тебя поле называется иначе — поправь
            .FirstOrDefaultAsync(ct);

        // Мигрируем каждый блоб отдельно
        await UploadColumn(
            db, "dbo.Photos", "Id", id, "Preview",
            keyColumn: "S3Key_Preview",
            etagColumn: "S3ETag_Preview",
            shaColumn: "Sha256_Preview",
            sizeColumn: "BlobSize_Preview",
            migratedAtColumn: "MigratedAt_Preview",
            s3Prefix: "photos/preview",
            alias: "preview",
            originalName: name,
            ct: ct);

        await UploadColumn(
            db, "dbo.Photos", "Id", id, "Thumbnail",
            keyColumn: "S3Key_Thumbnail",
            etagColumn: "S3ETag_Thumbnail",
            shaColumn: "Sha256_Thumbnail",
            sizeColumn: "BlobSize_Thumbnail",
            migratedAtColumn: "MigratedAt_Thumbnail",
            s3Prefix: "photos/thumbnail",
            alias: "thumbnail",
            originalName: name,
            ct: ct);
    }

    // -------- Faces row --------
    private async Task MigrateFaceRow(PhotoBankDbContext db, int id, CancellationToken ct)
    {
        await UploadColumn(
            db, "dbo.Faces", "Id", id, "Image",
            keyColumn: "S3Key_Image",
            etagColumn: "S3ETag_Image",
            shaColumn: "Sha256_Image",
            sizeColumn: "BlobSize_Image",
            migratedAtColumn: "MigratedAt_Image",
            s3Prefix: "faces/image",
            alias: "face",
            originalName: null,
            ct: ct);
    }

    // -------- Общий аплоад колонки --------
    private async Task UploadColumn(
        PhotoBankDbContext db,
        string table, string idColumn, int id, string blobColumn,
        string keyColumn, string etagColumn, string shaColumn, string sizeColumn, string migratedAtColumn,
        string s3Prefix, string alias, string? originalName, CancellationToken ct)
    {
        // если уже мигрировано — выходим
        var already = await db.Database.ExecuteScalarAsync(
            $"SELECT {keyColumn} FROM {table} WITH (NOLOCK) WHERE {idColumn}=@id",
            new SqlParameter("@id", SqlDbType.Int) { Value = id }, ct);
        if (already is string s && !string.IsNullOrWhiteSpace(s)) return;

        // есть ли blob вообще
        var lenObj = await db.Database.ExecuteScalarAsync(
            $"SELECT DATALENGTH([{blobColumn}]) FROM {table} WITH (NOLOCK) WHERE {idColumn}=@id",
            new SqlParameter("@id", SqlDbType.Int) { Value = id }, ct);

        var len = lenObj == DBNull.Value ? 0 : Convert.ToInt64(lenObj);
        if (len <= 0) return;

        // выгружаем во временный файл + считаем sha256
        var tmp = Path.Combine(_opt.TempDir, $"{table.Replace('.', '_')}_{id}_{blobColumn}.bin");
        var (sha256, size) = await DumpBlobToFileAndHash(db, table, idColumn, id, blobColumn, tmp, ct);

        // ключ
        var key = BuildKey(s3Prefix, id, ".jpg");

        // если уже есть объект с таким же sha256 — просто отмечаем
        if (await ExistsWithSameHash(key, sha256, ct) is string etExisting)
        {
            await MarkMigrated(db, table, idColumn, id, keyColumn, etagColumn, shaColumn, sizeColumn, migratedAtColumn,
                key, etExisting, sha256, size, ct);
            File.Delete(tmp);
            return;
        }

        // upload
        await using (var fs = File.OpenRead(tmp))
        {
            var putArgs = new PutObjectArgs()
                .WithBucket(_s3.Bucket)
                .WithObject(key)
                .WithStreamData(fs)
                .WithObjectSize(fs.Length)
                .WithContentType(ChooseContentType(alias))
                .WithHeaders(new Dictionary<string, string>
                {
                    ["X-Amz-Meta-sha256"] = sha256,
                    ["X-Amz-Meta-source-table"] = table,
                    ["X-Amz-Meta-source-id"] = id.ToString(),
                    ["X-Amz-Meta-alias"] = alias,
                    ["X-Amz-Meta-original-name"] = originalName ?? ""
                });

            var resp = await _minio.PutObjectAsync(putArgs, ct);
            await MarkMigrated(db, table, idColumn, id, keyColumn, etagColumn, shaColumn, sizeColumn, migratedAtColumn,
                key, resp.Etag, sha256, size, ct);
        }

        File.Delete(tmp);
    }

    private async Task<(string sha256, long size)> DumpBlobToFileAndHash(
        PhotoBankDbContext db,
        string table, string idColumn, int id, string blobColumn,
        string file, CancellationToken ct)
    {
        await using var con = (SqlConnection)db.Database.GetDbConnection();
        if (con.State != ConnectionState.Open) await con.OpenAsync(ct);

        await using var cmd = con.CreateCommand();
        cmd.CommandText = $"SELECT [{blobColumn}] FROM {table} WITH (NOLOCK) WHERE {idColumn}=@id";
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });

        await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleRow, ct);
        if (!await rdr.ReadAsync(ct)) throw new InvalidOperationException($"{table}:{id} not found");

        await using var stream = rdr.GetStream(0);
        using var sha = SHA256.Create();
        await using var fs = File.Create(file);

        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
        long total = 0;
        try
        {
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, read), ct);
                sha.TransformBlock(buffer, 0, read, null, 0);
                total += read;
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var hex = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
            return (hex, total);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task<string?> ExistsWithSameHash(string key, string sha256, CancellationToken ct)
    {
        try
        {
            var stat = await _minio.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_s3.Bucket).WithObject(key), ct);

            if (stat.MetaData != null &&
                stat.MetaData.TryGetValue("X-Amz-Meta-sha256", out var remoteSha) &&
                string.Equals(remoteSha, sha256, StringComparison.OrdinalIgnoreCase))
            {
                return stat.ETag;
            }

            return null;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return null;
        }
    }

    private async Task MarkMigrated(
        PhotoBankDbContext db,
        string table, string idColumn, int id,
        string keyColumn, string etagColumn, string shaColumn, string sizeColumn, string migratedAtColumn,
        string key, string etag, string sha256, long size, CancellationToken ct)
    {
        var sql = $@"
UPDATE {table}
   SET {keyColumn}=@k,
       {etagColumn}=@e,
       {shaColumn}=@s,
       {sizeColumn}=@sz,
       {migratedAtColumn}=SYSUTCDATETIME()
 WHERE {idColumn}=@id";

        await db.Database.ExecuteSqlRawAsync(sql,
            new SqlParameter("@k", SqlDbType.NVarChar, 512) { Value = key },
            new SqlParameter("@e", SqlDbType.NVarChar, 128) { Value = etag },
            new SqlParameter("@s", SqlDbType.Char, 64) { Value = sha256 },
            new SqlParameter("@sz", SqlDbType.BigInt) { Value = size },
            new SqlParameter("@id", SqlDbType.Int) { Value = id }, ct);
    }
}

// Небольшие помощники
static class DbRawExtensions
{
    public static async Task<object?> ExecuteScalarAsync(this DatabaseFacade db, string sql, SqlParameter param, CancellationToken ct)
    {
        await using var con = (SqlConnection)db.GetDbConnection();
        if (con.State != ConnectionState.Open) await con.OpenAsync(ct);

        await using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(param);
        return await cmd.ExecuteScalarAsync(ct);
    }
}
