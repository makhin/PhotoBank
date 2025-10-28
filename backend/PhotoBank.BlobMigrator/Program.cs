using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using PhotoBank.BlobMigrator;
using PhotoBank.DbContext.DbContext;
using System.Reflection;

// Configure Npgsql to treat DateTime with Kind=Unspecified as UTC
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
builder.Services.AddDbContextPool<PhotoBankDbContext>((sp, options) =>
{
    options.UseLoggerFactory(LoggerFactory.Create(loggingBuilder => loggingBuilder.AddDebug()));

    options.UseNpgsql(
        connectionString,
        npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(PhotoBankDbContext).GetTypeInfo().Assembly.GetName().Name);
            npgsql.UseNetTopologySuite();
            npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
            npgsql.CommandTimeout(60);
            npgsql.MaxBatchSize(128);
            npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
});
builder.Services.AddDbContextFactory<PhotoBankDbContext>();

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

// 7) HostedService
builder.Services.AddHostedService<BlobMigrationHostedService>();

await builder.Build().RunAsync();

namespace PhotoBank.BlobMigrator
{
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
        public string Endpoint { get; set; } = "http://192.168.1.63:9010";
        public bool UseSsl { get; set; }
        public string AccessKey { get; set; } = "alexandr";
        public string SecretKey { get; set; } = "12345678";
        public string Bucket { get; set; } = "photobank";
    }
}