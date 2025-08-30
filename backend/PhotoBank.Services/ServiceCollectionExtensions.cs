using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.Services.Events;

namespace PhotoBank.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoEvents(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PhotoCreated>());
        return services;
    }
}
