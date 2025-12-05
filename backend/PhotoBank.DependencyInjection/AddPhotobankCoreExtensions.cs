using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichment;
using PhotoBank.Services.ImageAnalysis;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Photos;
using PhotoBank.Services.Photos.Admin;
using PhotoBank.Services.Photos.Faces;
using PhotoBank.Services.Photos.Queries;
using PhotoBank.Services.Photos.Upload;
using PhotoBank.Services.Search;
using PhotoBank.Services.Translator;
using Polly;
using System;
using System.IO.Abstractions;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.Models;

namespace PhotoBank.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotobankCore(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IMinioClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
            return new MinioClient()
                .WithEndpoint(opts.Endpoint)
                .WithCredentials(opts.AccessKey, opts.SecretKey)
                .Build();
        });
        services.AddHostedService<MinioInitializationService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IFaceStorageService, FaceStorageService>();
        services.AddScoped<MinioObjectService>();
        services.AddScoped<IMediaUrlResolver, MediaUrlResolver>();
        services.AddScoped<ISearchFilterNormalizer, SearchFilterNormalizer>();
        services.AddScoped<PhotoFilterSpecification>();
        services.AddScoped<IPhotoQueryService, PhotoQueryService>();
        services.AddScoped<IPersonDirectoryService, PersonDirectoryService>();
        services.AddScoped<IPersonGroupService, PersonGroupService>();
        services.AddScoped<IFaceCatalogService, FaceCatalogService>();
        services.AddScoped<IPhotoDuplicateFinder, PhotoDuplicateFinder>();
        services.AddScoped<IPhotoFileSystemDuplicateChecker, PhotoFileSystemDuplicateChecker>();
        services.AddSingleton<UploadNameResolver>();
        services.AddScoped<IStorageUploadStrategy, ObjectStorageUploadStrategy>();
        services.AddScoped<IStorageUploadStrategy, FileSystemStorageUploadStrategy>();
        services.AddScoped<IPhotoIngestionService, PhotoIngestionService>();
        services.AddScoped<IPhotoAdminService, PhotoAdminService>();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<ISearchReferenceDataService, SearchReferenceDataService>();
        services.AddSingleton<IActiveEnricherProvider, ActiveEnricherProvider>();

        // Register IImageService required by PreviewEnricher
        // This stateless service handles image resizing operations
        // Use TryAdd to avoid duplicate registration if API/Console extensions already registered it
        services.TryAddSingleton<IImageService, ImageService>();

        // Register core enrichers needed by both API and Console
        // These are minimal enrichers that don't require heavy dependencies (Azure/AWS clients)
        services.AddTransient<IEnricher, MetadataEnricher>();
        services.AddTransient<IEnricher, ThumbnailEnricher>();
        services.AddTransient<IEnricher, PreviewEnricher>();
        services.AddTransient<IEnricher, DuplicateEnricher>();
        services.AddTransient<IImageMetadataReaderWrapper, ImageMetadataReaderWrapper>();

        // Register enrichment infrastructure (pipeline, catalog, re-enrichment service)
        // This allows API to use IReEnrichmentService and related services
        services.AddEnrichmentInfrastructure();

        // Stop condition for duplicate photos (cross-storage detection)
        services.AddSingleton<IEnrichmentStopCondition, DuplicateStopCondition>();

        // Stop condition for adult content:
        // Only stop if adult content is detected AND OpenRouter is enabled
        // (OpenRouter is used for caption generation which we want to avoid for adult content)
        services.AddEnrichmentStopCondition(
            "Adult content detected",
            (IServiceProvider sp, Photo photo, SourceDataDto _) =>
            {
                if (!photo.IsAdultContent)
                    return false;

                // Check if OpenRouter analyzer is active
                var imageAnalyzer = sp.GetService<IImageAnalyzer>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<object>>();

                if (imageAnalyzer == null)
                {
                    logger?.LogDebug("Adult content detected but no IImageAnalyzer registered - continuing enrichment");
                    return false; // No analyzer configured, don't stop
                }

                var shouldStop = imageAnalyzer.Kind == ImageAnalyzerKind.OpenRouter;
                logger?.LogInformation("Adult content detected. Image analyzer: {AnalyzerKind}, ShouldStop: {ShouldStop}",
                    imageAnalyzer.Kind, shouldStop);

                return shouldStop;
            },
            typeof(AdultEnricher));

        services.AddPhotoEvents();
        if (configuration != null)
        {
            services.AddOptions<TranslatorOptions>().Bind(configuration.GetSection("Translator"));
            services.AddOptions<MinioOptions>().Bind(configuration.GetSection("Minio"));
            services.Configure<S3Options>(configuration.GetSection("S3"));
        }
        else
        {
            services.AddOptions<TranslatorOptions>();
            services.AddOptions<MinioOptions>();
            services.AddOptions<S3Options>();
        }
        services.AddHttpClient<ITranslatorService, TranslatorService>()
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * attempt)));
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        return services;
    }
}
