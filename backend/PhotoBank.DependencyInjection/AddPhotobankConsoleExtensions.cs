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
using PhotoBank.Services.ObjectDetection;
using PhotoBank.Services.ObjectDetection.Abstractions;
using PhotoBank.Services.Recognition;

namespace PhotoBank.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotobankConsole(this IServiceCollection services, IConfiguration configuration)
    {
        const string computerVision = "ComputerVision";
        const string face = "Face";
        const string yoloOnnx = "YoloOnnx";
        const string nudeNetOnnx = "NudeNetOnnx";

        services.Configure<ComputerVisionOptions>(configuration.GetSection(computerVision));
        services.Configure<FaceApiOptions>(configuration.GetSection(face));
        services.Configure<YoloOnnxOptions>(configuration.GetSection(yoloOnnx));
        services.Configure<NudeNetOnnxOptions>(configuration.GetSection(nudeNetOnnx));
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
        services.AddScoped<IPhotoDeletionService, PhotoDeletionService>();
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
        services.AddTransient<IEnricher, CategoryEnricher>();

        // Object detection - unified approach with provider pattern
        // Register YOLO ONNX with CUDA GPU acceleration (no CPU fallback)
        var yoloOptions = configuration.GetSection(yoloOnnx).Get<YoloOnnxOptions>();
        if (yoloOptions?.Enabled == true && !string.IsNullOrWhiteSpace(yoloOptions.ModelPath))
        {
            // Register YoloOnnxService as singleton (manages InferenceSession with CUDA)
            // The service uses ONNX Runtime with CUDA execution provider for GPU acceleration
            services.AddSingleton<IYoloOnnxService, YoloOnnxService>();

            // Register YOLO ONNX provider
            services.AddTransient<IObjectDetectionProvider, YoloOnnxObjectDetectionProvider>();

            Console.WriteLine($"YOLO ONNX object detection provider initialized with CUDA GPU acceleration.");
            Console.WriteLine($"Model path: {yoloOptions.ModelPath}");
        }
        else
        {
            // ONNX not enabled, use Azure-based object detection
            services.AddTransient<IObjectDetectionProvider, AzureObjectDetectionProvider>();
        }

        // Register unified object property enricher (uses the provider registered above)
        services.AddTransient<IEnricher, UnifiedObjectPropertyEnricher>();

        // NudeNet detection - local ONNX model (YOLOv8-based nudity detection)
        var nudeNetOptions = configuration.GetSection(nudeNetOnnx).Get<NudeNetOnnxOptions>();
        if (nudeNetOptions?.Enabled == true && !string.IsNullOrWhiteSpace(nudeNetOptions.ModelPath))
        {
            if (System.IO.File.Exists(nudeNetOptions.ModelPath))
            {
                try
                {
                    // Validate ONNX model by actually loading it
                    // This ensures the model file is valid and compatible before registering services
                    using (var sessionOptions = new Microsoft.ML.OnnxRuntime.SessionOptions())
                    using (var validationSession = new Microsoft.ML.OnnxRuntime.InferenceSession(nudeNetOptions.ModelPath, sessionOptions))
                    {
                        // Model loaded successfully, it's valid
                        Console.WriteLine($"NudeNet ONNX model validated: {nudeNetOptions.ModelPath}");
                        Console.WriteLine($"Input resolution: {nudeNetOptions.InputResolution}x{nudeNetOptions.InputResolution}");
                    }

                    // Register NudeNet detector as singleton (it manages its own session)
                    services.AddSingleton<INudeNetDetector, NudeNetDetector>();

                    // Register Adult enricher
                    services.AddTransient<IEnricher, AdultEnricher>();

                    Console.WriteLine("NudeNet ONNX enricher initialized successfully with CUDA GPU acceleration.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WARNING: Failed to load NudeNet ONNX model: {ex.GetType().Name}: {ex.Message}");
                    Console.WriteLine("This could be caused by:");
                    Console.WriteLine("  - Corrupt or incompatible model file");
                    Console.WriteLine("  - Missing ONNX Runtime native libraries");
                    Console.WriteLine("  - Incompatible model format or version");
                    Console.WriteLine("Adult enricher will be disabled.");

                    // Register disabled detector to keep Adult enricher resolvable
                    services.AddSingleton<INudeNetDetector, DisabledNudeNetDetector>();
                    services.AddTransient<IEnricher, AdultEnricher>();
                }
            }
            else
            {
                Console.WriteLine($"WARNING: NudeNet ONNX model file not found at: {nudeNetOptions.ModelPath}. Adult enricher will be disabled.");

                // Register disabled detector to keep Adult enricher resolvable
                services.AddSingleton<INudeNetDetector, DisabledNudeNetDetector>();
                services.AddTransient<IEnricher, AdultEnricher>();
            }
        }
        else
        {
            // NudeNet ONNX not enabled, Adult enricher will be disabled
            services.AddSingleton<INudeNetDetector, DisabledNudeNetDetector>();
            services.AddTransient<IEnricher, AdultEnricher>();
        }

        services.AddTransient<IEnricher, UnifiedFaceEnricher>();

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
