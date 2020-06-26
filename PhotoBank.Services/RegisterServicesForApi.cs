using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.Services.Api;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.Services
{
    public static class RegisterServicesForApi
    {
        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IPhotoService, PhotoService>();
        }
    }
}
