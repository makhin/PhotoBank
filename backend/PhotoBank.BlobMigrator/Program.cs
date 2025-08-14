using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using PhotoBank.DbContext.DbContext;
using System.Runtime.InteropServices;
using ImageMagick;

var builder = Host.CreateApplicationBuilder(args);

// 1) Явно подключаем application.json (оставьте имя таким или переименуйте в appsettings.json)
builder.Configuration.AddJsonFile("application.json", optional: false, reloadOnChange: true);

// 2) Биндим options
builder.Services.Configure<BlobMigrationOptions>(builder.Configuration.GetSection("BlobMigration"));
builder.Services.Configure<S3Options>(builder.Configuration.GetSection("S3"));

// 3) Строка подключения
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

// 4) EF Core: фабрика контекстов для многопоточности
builder.Services.AddDbContextFactory<PhotoBankDbContext>(opt =>
{
    opt.UseSqlServer(connectionString, sql => sql.UseNetTopologySuite());
});

// 5) MinIO/S3 клиент
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var s3 = sp.GetRequiredService<IOptions<S3Options>>().Value;

    var endpoint = s3.Endpoint
        .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
        .Replace("http://", "", StringComparison.OrdinalIgnoreCase);

    return new MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(s3.AccessKey, s3.SecretKey)
        .WithSSL(s3.UseSsl)
        .Build();
});

// 6) Ограничения ресурсов для Magick.NET (безопасные лимиты по умолчанию — при необходимости поднимите)
ResourceLimits.Memory = 256 * 1024 * 1024; // 256 MB
ResourceLimits.Map    = 512 * 1024 * 1024; // 512 MB
if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // На Linux иногда полезно ограничить ещё и Disk, чтобы Magick не лез во временный swap-файл.
    ResourceLimits.Disk = 1024L * 1024 * 1024; // 1 GB
}

// 7) HostedService
builder.Services.AddHostedService<BlobMigrationHostedService>();

await builder.Build().RunAsync();


// ---------------- Options ----------------
public sealed class BlobMigrationOptions
{
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 200;
    public int Concurrency { get; set; } = 3;
    public string TempDir { get; set; } = "tmp-migrate";
}

public sealed class S3Options
{
    public string Endpoint { get; set; } = "http://127.0.0.1:9000";
    public bool UseSsl { get; set; }
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Bucket { get; set; } = "photobank";
}