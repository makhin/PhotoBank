using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.Services.Events;

namespace PhotoBank.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotoEvents(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PhotoCreated>());
        return services;
    }
}
