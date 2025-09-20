namespace PhotoBank.Api.MinimalApi;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PhotoBank.Services.Api;

public static class PhotoServiceEndpointExtensions
{
    public static RouteHandlerBuilder MapPhotoServiceCollection<TDto>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<IPhotoService, CancellationToken, Task<IEnumerable<TDto>>> handler,
        string? tag = null)
    {
        var route = endpoints.MapGet(pattern, async (IPhotoService service, CancellationToken cancellationToken) =>
        {
            var result = await handler(service, cancellationToken);
            return Results.Ok(result);
        });

        route.RequireAuthorization();
        route.Produces<IEnumerable<TDto>>(StatusCodes.Status200OK);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            route.WithTags(tag);
        }

        return route;
    }

    public static RouteHandlerBuilder MapPhotoServiceCollection<TDto>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<IPhotoService, Task<IEnumerable<TDto>>> handler,
        string? tag = null)
    {
        return endpoints.MapPhotoServiceCollection(
            pattern,
            (service, _) => handler(service),
            tag);
    }
}
