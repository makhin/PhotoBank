using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using PhotoBank.AccessControl;
using PhotoBank.DbContext.DbContext;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PhotoBank.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhotobankApi(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        bool configureCors = false,
        Action<SwaggerGenOptions>? configureSwagger = null)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IImageService, ImageService>();
        services.AddSingleton<IS3ResourceService, S3ResourceService>();
        services.AddTransient<IFaceStorageService, FaceStorageService>();
        services.AddScoped<IEffectiveAccessProvider, EffectiveAccessProvider>();
        services.TryAddScoped<ICurrentUser, CurrentUser>();
        services.AddDefaultIdentity<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<PhotoBankDbContext>();

        if (configuration != null)
        {
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
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
                };
            });
        }

        if (configureCors)
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
        }

        if (configureSwagger != null)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
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

                configureSwagger(options);
            });
        }

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
