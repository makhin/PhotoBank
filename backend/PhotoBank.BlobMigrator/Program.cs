using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Minio;
using PhotoBank.DbContext.DbContext;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<PhotoBankDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.UseNetTopologySuite()));

builder.Services.Configure<BlobMigrationOptions>(
    builder.Configuration.GetSection("BlobMigration"));
builder.Services.Configure<S3Options>(builder.Configuration.GetSection("S3"));

builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<S3Options>>().Value;
    return new MinioClient()
        .WithEndpoint(opt.Endpoint.Replace("https://", "").Replace("http://", ""))
        .WithCredentials(opt.AccessKey, opt.SecretKey)
        .WithSSL(opt.UseSsl)
        .Build();
});

builder.Services.AddHostedService<BlobMigrationHostedService>();

var host = builder.Build();
await host.RunAsync();
