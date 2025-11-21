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
using PhotoBank.Services.ImageAnalysis;
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
        services.Configure<ImageAnalyzerOptions>(configuration.GetSection(ImageAnalyzerOptions.SectionName));
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));

        // Register IImageAnalyzer based on configuration
        var imageAnalyzerOptions = configuration.GetSection(ImageAnalyzerOptions.SectionName).Get<ImageAnalyzerOptions>()
            ?? new ImageAnalyzerOptions();

        if (imageAnalyzerOptions.Provider == ImageAnalyzerKind.Ollama)
        {
            services.AddSingleton<IImageAnalyzer, OllamaImageAnalyzer>();
        }
        else
        {
            // Only register Azure Computer Vision client when using Azure provider
            services.AddSingleton<IComputerVisionClient, ComputerVisionClient>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<ComputerVisionOptions>>().Value;
                var credentials = new ApiKeyServiceClientCredentials(options.Key);
                return new ComputerVisionClient(credentials)
                {
                    Endpoint = options.Endpoint
                };
            });
            services.AddSingleton<IImageAnalyzer, AzureImageAnalyzer>();
        }

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
            if (System.IO.File.Exists(yoloOptions.ModelPath))
            {
                // Register PredictionEngine for ONNX model
                // Note: PredictionEngine.Predict() is thread-safe for concurrent read operations
                services.AddSingleton(provider =>
                {
                    var mlContext = new MLContext();

                    // Create input schema for YOLO (3 channels, 640x640 input)
                    var dataView = mlContext.Data.LoadFromEnumerable(new List<YoloImageInput>());

                    // Build pipeline with ONNX model
                    var pipeline = mlContext.Transforms.ApplyOnnxModel(
                        outputColumnName: "output0",
                        inputColumnName: "images",
                        modelFile: yoloOptions.ModelPath);

                    // Fit the pipeline to create the model
                    var model = pipeline.Fit(dataView);

                    // Create PredictionEngine (thread-safe for Predict operations)
                    return mlContext.Model.CreatePredictionEngine<YoloImageInput, YoloOutput>(model);
                });

                // Register YoloOnnxService as transient (engine is singleton)
                services.AddTransient<IYoloOnnxService, YoloOnnxService>();

                // Register ONNX enricher as concrete type (required for EnricherTypeCatalog)
                services.AddTransient<IEnricher, OnnxObjectDetectionEnricher>();
            }
            else
            {
                // Model file not found - fallback to Azure Computer Vision
                // Note: Cannot use ILogger here as it would require BuildServiceProvider() which leaks resources
                Console.WriteLine($"WARNING: ONNX model file not found at: {yoloOptions.ModelPath}. Falling back to Azure Computer Vision.");

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

        // Register enrichment infrastructure (pipeline, catalog, re-enrichment service)
        // This uses all enrichers registered above
        services.AddEnrichmentInfrastructure();

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
