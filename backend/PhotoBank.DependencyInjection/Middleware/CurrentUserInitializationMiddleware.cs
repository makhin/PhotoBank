using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PhotoBank.AccessControl;

namespace PhotoBank.DependencyInjection.Middleware;

public sealed class CurrentUserInitializationMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserInitializationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserAccessor accessor)
    {
        await accessor.GetCurrentUserAsync(context.RequestAborted).ConfigureAwait(false);
        await _next(context).ConfigureAwait(false);
    }
}
