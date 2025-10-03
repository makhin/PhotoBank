using System;
using System.Text;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using PhotoBank.DependencyInjection.Swagger;

namespace PhotoBank.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotobankApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IImageService, ImageService>();
        services.AddScoped<IEffectiveAccessProvider, EffectiveAccessProvider>();
        services.TryAddScoped<ICurrentUser, CurrentUser>();
        services.AddDefaultIdentity<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<PhotoBankDbContext>();

        var jwtSection = configuration.GetSection("Jwt");
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                NameClaimType = ClaimTypes.NameIdentifier,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
            };
        });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    public static IServiceCollection AddPhotobankCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddPhotobankMvc(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
        });

        services.AddProblemDetails();

        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("DefaultConnection")!,
                name: "sql",
                tags: new[] { "ready" },
                failureStatus: HealthStatus.Unhealthy);

        services.AddControllers(options =>
            {
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
            });

        services.AddFluentValidationAutoValidation();

        var validatorType = Type.GetType("PhotoBank.Api.Validators.LoginRequestDtoValidator, PhotoBank.Api");
        if (validatorType is not null)
        {
            services.AddValidatorsFromAssemblyContaining(validatorType);
        }

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
                new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
        });

        return services;
    }

    public static IServiceCollection AddPhotobankSwagger(
        this IServiceCollection services,
        Action<SwaggerGenOptions>? configure = null)
    {
        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        var version = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "1.0.0";
        version = version.Split('+')[0];
        var parts = version.Split('.');
        if (parts.Length >= 3)
        {
            version = string.Join('.', parts[0], parts[1], parts[2]);
        }
        const string title = "PhotoBank.Api";

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = version
            });

            options.SchemaFilter<StringEnumSchemaFilter>();
            options.DocumentFilter<ServersDocumentFilter>();

            options.CustomOperationIds(apiDesc =>
            {
                if (apiDesc.ActionDescriptor is ControllerActionDescriptor descriptor)
                {
                    var controllerName = descriptor.ControllerName;
                    var actionName = descriptor.ActionName;
                    return $"{controllerName}_{actionName}";
                }

                return null;
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "¬ведите токен JWT так: Bearer {токен}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            configure?.Invoke(options);
        });

        return services;
    }
}
