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
using ApiKeyServiceClientCredentials = Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials;

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

            services.AddSingleton<IInsightFaceApiClient, InsightFaceApiClient.InsightFaceApiClient>();
            services.AddTransient<IRecognitionService, RecognitionService>();

            services.AddTransient<EnricherResolver>(serviceProvider => repository =>
            {
                return repository.GetAll()
                    .Where(e => e.IsActive)
                    .Select(e => "PhotoBank.Services.Enrichers." + e.Name).AsEnumerable()
                    .Select(Type.GetType)
                    .Select(enricher => enricher != null
                        ? (IEnricher) serviceProvider.GetService(enricher)
                        : throw new NotSupportedException());
            });
        }
    }
}
