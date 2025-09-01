using System;
using ApiKeyServiceClientCredentials = Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials;
using System.Linq;
using Amazon.Rekognition;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.AccessControl;
using PhotoBank.InsightFaceApiClient;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.FaceRecognition;
using PhotoBank.Services.Recognition;

namespace PhotoBank.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotobankConsole(this IServiceCollection services, IConfiguration configuration)
    {
        const string computerVision = "ComputerVision";
        const string face = "Face";

        services.AddSingleton<IComputerVisionClient, ComputerVisionClient>(provider =>
        {
            var key = configuration.GetSection(computerVision)["Key"];
            var credentials = new ApiKeyServiceClientCredentials(key);
            return new ComputerVisionClient(credentials)
            {
                Endpoint = configuration.GetSection(computerVision)["Endpoint"]
            };
        });

        services.AddSingleton<IFaceClient, FaceClient>(provider =>
        {
            var key = configuration.GetSection(face)["Key"];
            var credentials = new ApiKeyServiceClientCredentials(key);
            return new FaceClient(credentials)
            {
                Endpoint = configuration.GetSection(face)["Endpoint"]
            };
        });

        services.AddSingleton(typeof(AmazonRekognitionClient));
        services.AddTransient<IDependencyExecutor, DependencyExecutor>();
        services.AddTransient<IFaceService, FaceService>();
        services.AddTransient<IFacePreviewService, FacePreviewService>();
        services.AddTransient<IFaceServiceAws, FaceServiceAws>();
        services.AddTransient<IFaceStorageService, FaceStorageService>();
        services.AddTransient<IPhotoProcessor, PhotoProcessor>();
        services.AddTransient<IPhotoService, PhotoService>();
        services.AddSingleton<ICurrentUser, DummyCurrentUser>();
        services.AddTransient<IImageService, ImageService>();
        services.AddTransient<ISyncService, SyncService>();
        services.AddTransient<IEnricher, MetadataEnricher>();
        services.AddTransient<IEnricher, ThumbnailEnricher>();
        services.AddTransient<IEnricher, PreviewEnricher>();
        services.AddTransient<IEnricher, AnalyzeEnricher>();
        services.AddTransient<IEnricher, ColorEnricher>();
        services.AddTransient<IEnricher, CaptionEnricher>();
        services.AddTransient<IEnricher, TagEnricher>();
        services.AddTransient<IEnricher, ObjectPropertyEnricher>();
        services.AddTransient<IEnricher, AdultEnricher>();
        services.AddTransient<IEnricher, FaceEnricher>();
        services.AddTransient<IEnricher, FaceEnricherAws>();
        services.AddTransient<IImageMetadataReaderWrapper, ImageMetadataReaderWrapper>();
        services.AddFaceRecognition(configuration);
        services.AddScoped<UnifiedFaceService>();
        services.AddScoped<IFaceService, FaceService>();
        services.AddSingleton<IInsightFaceApiClient, InsightFaceApiClient.InsightFaceApiClient>();
        services.AddTransient<IRecognitionService, RecognitionService>();
        services.AddSingleton<EnricherResolver>(provider =>
        {
            var enricherTypes = typeof(IEnricher).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(IEnricher).IsAssignableFrom(t))
                .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

            return repository =>
            {
                return repository.GetAll()
                    .Where(e => e.IsActive)
                    .AsEnumerable()
                    .Select(e =>
                    {
                        if (!enricherTypes.TryGetValue(e.Name, out var type))
                            throw new NotSupportedException($"Enricher '{e.Name}' not found in loaded assemblies.");
                        return (IEnricher)provider.GetRequiredService(type);
                    });
            };
        });
        return services;
    }
}
