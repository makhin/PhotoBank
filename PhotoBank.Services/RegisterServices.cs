using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PhotoBank.Services
{
    public static class RegisterServices
    {
        const string ComputerVision = "ComputerVision";

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

            services.AddSingleton<IGeoWrapper, GeoWrapper>();
            services.AddTransient<IImageEncoder, ImageEncoder>();
            services.AddTransient<IPhotoProcessor, PhotoProcessor>();
            services.AddScoped<IEnricher<string>, MetadataEnricher>();
            services.AddScoped<IEnricher<string>, PreviewEnricher>();
            services.AddScoped<IEnricher<ImageAnalysis>, ColorEnricher>();
            services.AddScoped<IEnricher<ImageAnalysis>, CaptionEnricher>();
            services.AddScoped<IEnricher<ImageAnalysis>, TagEnricher>();
            services.AddScoped<IEnricher<ImageAnalysis>, CaptionEnricher>();
            services.AddScoped<IEnricher<ImageAnalysis>, ObjectPropertyEnricher>();
            services.AddScoped<IEnricher<ImageAnalysis>, FaceEnricher>();
            services.AddScoped<IEnricher<ImageAnalysis>, AdultEnricher>();
        }
    }
}
