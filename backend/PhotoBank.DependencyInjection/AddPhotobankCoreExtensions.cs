using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Minio;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Internal;
using PhotoBank.Services.Enrichment;
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
        services.AddScoped<IPhotoDeletionService, PhotoDeletionService>();
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
        services.AddTransient<IImageMetadataReaderWrapper, ImageMetadataReaderWrapper>();

        // Register enrichment infrastructure (pipeline, catalog, re-enrichment service)
        // This allows API to use IReEnrichmentService and related services
        services.AddEnrichmentInfrastructure();

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
