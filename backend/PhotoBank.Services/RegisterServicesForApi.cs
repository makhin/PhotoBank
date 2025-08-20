using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PhotoBank.Repositories;
using PhotoBank.Services.Api;
using PhotoBank.AccessControl;

namespace PhotoBank.Services
{
    public static class RegisterServicesForApi
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IPhotoService, PhotoService>();
            services.TryAddScoped<ICurrentUser, DummyCurrentUser>();
            services.AddSingleton<ITokenService, TokenService>();
            services.AddSingleton<IImageService, ImageService>();
        }
    }
}
