using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace PhotoBank.Services.Internal;

/// <summary>
/// Сервис для инициализации MinIO bucket при старте приложения.
/// Создает bucket если не существует и устанавливает публичную политику для чтения.
/// </summary>
public sealed class MinioInitializationService : IHostedService
{
    private readonly IMinioClient _minioClient;
    private readonly S3Options _s3Options;
    private readonly ILogger<MinioInitializationService> _logger;

    public MinioInitializationService(
        IMinioClient minioClient,
        IOptions<S3Options> s3Options,
        ILogger<MinioInitializationService> logger)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _s3Options = s3Options?.Value ?? throw new ArgumentNullException(nameof(s3Options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await EnsureBucketExistsAsync(cancellationToken);
            await SetPublicReadPolicyAsync(cancellationToken);
            _logger.LogInformation("MinIO bucket '{Bucket}' initialized successfully with public read policy", _s3Options.Bucket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MinIO bucket '{Bucket}'", _s3Options.Bucket);
            // Не бросаем исключение, чтобы не прерывать запуск приложения
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_s3Options.Bucket),
            cancellationToken);

        if (!bucketExists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_s3Options.Bucket),
                cancellationToken);
            _logger.LogInformation("Created MinIO bucket '{Bucket}'", _s3Options.Bucket);
        }
        else
        {
            _logger.LogInformation("MinIO bucket '{Bucket}' already exists", _s3Options.Bucket);
        }
    }

    private async Task SetPublicReadPolicyAsync(CancellationToken cancellationToken)
    {
        // Создаем политику доступа: публичное чтение (GetObject) для всех объектов в bucket
        var policy = new
        {
            Version = "2012-10-17",
            Statement = new[]
            {
                new
                {
                    Effect = "Allow",
                    Principal = new { AWS = new[] { "*" } },
                    Action = new[] { "s3:GetObject" },
                    Resource = new[] { $"arn:aws:s3:::{_s3Options.Bucket}/*" }
                }
            }
        };

        var policyJson = JsonSerializer.Serialize(policy);

        await _minioClient.SetPolicyAsync(
            new SetPolicyArgs()
                .WithBucket(_s3Options.Bucket)
                .WithPolicy(policyJson),
            cancellationToken);

        _logger.LogInformation("Set public read policy for MinIO bucket '{Bucket}'", _s3Options.Bucket);
    }
}
