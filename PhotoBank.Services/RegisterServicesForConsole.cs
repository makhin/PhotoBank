using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.Repositories;
using PhotoBank.Services.Api;
using PhotoBank.Services.Enrichers;
using ApiKeyServiceClientCredentials = Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials;

namespace PhotoBank.Services
{
    public static class RegisterServicesForConsole
    {
        private const string ComputerVision = "ComputerVision";
        private const string Face = "Face";

        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
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

            services.AddSingleton<IOrderResolver<IEnricher>, OrderResolver<IEnricher>>();
            services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            services.AddScoped<IFaceService, FaceService>();

            services.AddScoped<IPhotoProcessor, PhotoProcessor>();
            services.AddScoped<IPhotoService, PhotoService>();
            services.AddScoped<ISyncService, SyncService>();

            services.AddScoped<IEnricher, MetadataEnricher>();
            services.AddScoped<IEnricher, ThumbnailEnricher>();
            services.AddScoped<IEnricher, PreviewEnricher>();
            services.AddScoped<IEnricher, AnalyzeEnricher>();
            services.AddScoped<IEnricher, ColorEnricher>();
            services.AddScoped<IEnricher, CaptionEnricher>();
            services.AddScoped<IEnricher, TagEnricher>();
            services.AddScoped<IEnricher, CaptionEnricher>();
            services.AddScoped<IEnricher, ObjectPropertyEnricher>();
            services.AddScoped<IEnricher, AdultEnricher>();
            services.AddScoped<IEnricher, FaceEnricher>();
        }
    }
}
