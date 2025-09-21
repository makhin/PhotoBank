using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PhotoBank.IntegrationTests.Infra;

public static class TestAuthenticationDefaults
{
    public const string SchemeName = "IntegrationTest";
    public const string UserHeader = "X-Test-User";
    public const string RolesHeader = "X-Test-Roles";
}

public sealed class TestAuthenticationOptions
{
    public string SchemeName { get; set; } = TestAuthenticationDefaults.SchemeName;
    public bool ConfigureFallbackPolicy { get; set; }
}

public static class TestAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddTestAuthentication(
        this IServiceCollection services,
        Action<TestAuthenticationOptions>? configure = null)
    {
        var options = new TestAuthenticationOptions();
        configure?.Invoke(options);

        services.AddAuthentication(authOptions =>
            {
                authOptions.DefaultAuthenticateScheme = options.SchemeName;
                authOptions.DefaultChallengeScheme = options.SchemeName;
                authOptions.DefaultScheme = options.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(options.SchemeName, _ => { });

        if (options.ConfigureFallbackPolicy)
        {
            services.PostConfigure<AuthorizationOptions>(authorizationOptions =>
            {
                authorizationOptions.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(options.SchemeName)
                    .RequireAuthenticatedUser()
                    .Build();
            });
        }

        return services;
    }
}

internal sealed class HeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TestAuthenticationDefaults.UserHeader, out var userValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var user = userValues.ToString();
        if (string.IsNullOrWhiteSpace(user))
        {
            return Task.FromResult(AuthenticateResult.Fail("User header missing"));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user),
            new(ClaimTypes.Name, user)
        };

        if (Request.Headers.TryGetValue(TestAuthenticationDefaults.RolesHeader, out var rolesValues))
        {
            var roles = rolesValues.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
