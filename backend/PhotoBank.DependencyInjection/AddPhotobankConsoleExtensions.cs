using System;
using ApiKeyServiceClientCredentials = Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.Rekognition;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ML;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.ML;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Enrichment;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Onnx;
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
        const string yoloOnnx = "YoloOnnx";

        services.Configure<ComputerVisionOptions>(configuration.GetSection(computerVision));
        services.Configure<FaceApiOptions>(configuration.GetSection(face));
        services.Configure<YoloOnnxOptions>(configuration.GetSection(yoloOnnx));

        services.AddSingleton<IComputerVisionClient, ComputerVisionClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ComputerVisionOptions>>().Value;
            var credentials = new ApiKeyServiceClientCredentials(options.Key);
            return new ComputerVisionClient(credentials)
            {
                Endpoint = options.Endpoint
            };
        });

        services.AddSingleton<IFaceClient, FaceClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<FaceApiOptions>>().Value;
            var credentials = new ApiKeyServiceClientCredentials(options.Key);
            return new FaceClient(credentials)
            {
                Endpoint = options.Endpoint
            };
        });

        services.AddSingleton(typeof(AmazonRekognitionClient));
        services.AddTransient<IFaceService, FaceService>();
        services.AddTransient<IFacePreviewService, FacePreviewService>();
        services.AddTransient<IFaceServiceAws, FaceServiceAws>();
        services.AddTransient<IPhotoProcessor, PhotoProcessor>();
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

        // Object detection enrichers - use ONNX-based or Azure-based depending on configuration
        // IMPORTANT: Must register as concrete type (not factory) so enricher appears in EnricherTypeCatalog
        var yoloOptions = configuration.GetSection(yoloOnnx).Get<YoloOnnxOptions>();
        if (yoloOptions?.Enabled == true && !string.IsNullOrWhiteSpace(yoloOptions.ModelPath))
        {
            if (File.Exists(yoloOptions.ModelPath))
            {
                // Register PredictionEnginePool for thread-safe YOLO inference
                // Build ML.NET pipeline with ONNX model (FromUri expects .zip, not .onnx)
                services.AddPredictionEnginePool<YoloImageInput, YoloOutput>()
                    .FromFile(
                        filePath: yoloOptions.ModelPath,
                        watchForChanges: false,
                        modelLoader: (mlContext, path) =>
                        {
                            // Create input schema for YOLO (3 channels, 640x640 input)
                            var dataView = mlContext.Data.LoadFromEnumerable(new List<YoloImageInput>());

                            // Build pipeline with ONNX model
                            var pipeline = mlContext.Transforms.ApplyOnnxModel(
                                outputColumnName: "output0",
                                inputColumnName: "images",
                                modelFile: path);

                            // Fit the pipeline to create the model
                            return pipeline.Fit(dataView);
                        });

                // Register YoloOnnxService as transient (pool is singleton)
                services.AddTransient<IYoloOnnxService, YoloOnnxService>();

                // Register ONNX enricher as concrete type (required for EnricherTypeCatalog)
                services.AddTransient<IEnricher, OnnxObjectDetectionEnricher>();
            }
            else
            {
                // Log warning if model file not found
                var logger = services.BuildServiceProvider().GetService<ILogger<YoloOnnxService>>();
                logger?.LogWarning("ONNX model file not found at: {ModelPath}. Falling back to Azure Computer Vision.", yoloOptions.ModelPath);

                // Fallback to Azure-based object detection
                services.AddTransient<IEnricher, ObjectPropertyEnricher>();
            }
        }
        else
        {
            // ONNX not enabled, use Azure-based object detection
            services.AddTransient<IEnricher, ObjectPropertyEnricher>();
        }

        services.AddTransient<IEnricher, AdultEnricher>();

        // Face enrichers - use UnifiedFaceEnricher for new code
        services.AddTransient<IEnricher, UnifiedFaceEnricher>();
        // Legacy enrichers - kept for backward compatibility, will be removed in future versions
#pragma warning disable CS0618 // Type or member is obsolete
        services.AddTransient<IEnricher, FaceEnricher>();
        services.AddTransient<IEnricher, FaceEnricherAws>();
#pragma warning restore CS0618 // Type or member is obsolete

        services.AddTransient<IImageMetadataReaderWrapper, ImageMetadataReaderWrapper>();
        services.AddFaceRecognition(configuration);
        services.AddScoped<IUnifiedFaceService, UnifiedFaceService>();
        services.AddScoped<IFaceService, FaceService>();
        services.AddTransient<IRecognitionService, RecognitionService>();
        services.TryAddSingleton<IActiveEnricherProvider, ActiveEnricherProvider>();
        services.AddOptions<EnrichmentPipelineOptions>();

        var enricherTypes = services
            .Where(d => d.ServiceType == typeof(IEnricher) && d.ImplementationType is not null)
            .Select(d => d.ImplementationType!)
            .Distinct()
            .ToArray();

        services.AddSingleton(_ => new EnricherTypeCatalog(enricherTypes));
        services.AddSingleton<IEnrichmentPipeline, EnrichmentPipeline>();
        services.AddSingleton<EnricherResolver>(provider =>
        {
            var activeEnricherProvider = provider.GetRequiredService<IActiveEnricherProvider>();

            return repository =>
            {
                var activeTypes = activeEnricherProvider.GetActiveEnricherTypes(repository);
                return activeTypes.Select(type => (IEnricher)provider.GetRequiredService(type));
            };
        });
        return services;
    }
}
