using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.Services
{
    public static class RegisterServicesForConsole
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
            services.AddTransient<IOrderResolver<IEnricher>, OrderResolver<IEnricher>>();
            services.AddTransient<IImageEncoder, ImageEncoder>();
            services.AddTransient<IPhotoProcessor, PhotoProcessor>();
            services.AddScoped<IEnricher, MetadataEnricher>();
            services.AddScoped<IEnricher, PreviewEnricher>();
            services.AddScoped<IEnricher, ColorEnricher>();
            services.AddScoped<IEnricher, CaptionEnricher>();
            services.AddScoped<IEnricher, TagEnricher>();
            services.AddScoped<IEnricher, CaptionEnricher>();
            services.AddScoped<IEnricher, ObjectPropertyEnricher>();
            services.AddScoped<IEnricher, FaceEnricher>();
            services.AddScoped<IEnricher, AdultEnricher>();
        }
    }
}
