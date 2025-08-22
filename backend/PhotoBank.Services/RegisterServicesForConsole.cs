using System;
using System.Linq;
using Amazon.Rekognition;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.InsightFaceApiClient;
using PhotoBank.Repositories;
using PhotoBank.Services.Api;
using PhotoBank.Services.Enrichers;
using PhotoBank.Services.Enrichers.Services;
using PhotoBank.Services.Recognition;
using PhotoBank.Services.Translator;
using PhotoBank.AccessControl;
using ApiKeyServiceClientCredentials = Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials;
using Polly;

namespace PhotoBank.Services
{
    public static class RegisterServicesForConsole
    {
        private const string ComputerVision = "ComputerVision";
        private const string Face = "Face";

        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();
            services.AddSingleton<IComputerVisionClient, ComputerVisionClient>(provider =>
            {
                var key = configuration.GetSection(ComputerVision)["Key"];
                var credentials = new ApiKeyServiceClientCredentials(key);
                return new ComputerVisionClient(credentials)
                {
                    Endpoint = configuration.GetSection(ComputerVision)["Endpoint"]
                };
            });

            services.AddSingleton<IFaceClient, FaceClient>(provider =>
            {
                var key = configuration.GetSection(Face)["Key"];
                var credentials = new ApiKeyServiceClientCredentials(key);
                return new FaceClient(credentials)
                {
                    Endpoint = configuration.GetSection(Face)["Endpoint"]
                };
            });

            services.AddSingleton(typeof(AmazonRekognitionClient));
            services.AddTransient<IDependencyExecutor, DependencyExecutor>();

            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));

            services.AddTransient<IFaceService, FaceService>();
            services.AddTransient<IFacePreviewService, FacePreviewService>();
            services.AddTransient<IFaceServiceAws, FaceServiceAws>();

            services.AddTransient<IPhotoProcessor, PhotoProcessor>();
            services.AddTransient<IPhotoService, PhotoService>();
            services.AddSingleton<ICurrentUser, DummyCurrentUser>();
            services.AddTransient<IImageService, ImageService>();
            services.AddTransient<ISyncService, SyncService>();
            services.AddOptions<TranslatorOptions>().Bind(configuration.GetSection("Translator"));
            services.AddHttpClient<ITranslatorService, TranslatorService>()
                .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * attempt)));

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

            services.AddSingleton<IInsightFaceApiClient, InsightFaceApiClient.InsightFaceApiClient>();
            services.AddTransient<IRecognitionService, RecognitionService>();

            services.AddSingleton<EnricherResolver>(provider =>
            {
                // Построим карту доступных энричеров один раз
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
        }
    }
}
