using System;
using Amazon;
using Amazon.Rekognition;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoBank.Services.FaceRecognition.Abstractions;
using PhotoBank.Services.FaceRecognition.Aws;
using PhotoBank.Services.FaceRecognition.Azure;
using PhotoBank.Services.FaceRecognition.Local;

namespace PhotoBank.Services.FaceRecognition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFaceRecognition(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<FaceProviderOptions>(cfg.GetSection("FaceProvider"));
        services.Configure<LocalInsightFaceOptions>(cfg.GetSection("LocalInsightFace"));
        services.Configure<RekognitionOptions>(cfg.GetSection("AwsRekognition"));
        services.Configure<AzureFaceOptions>(cfg.GetSection("AzureFace"));

        services.AddHttpClient<ILocalInsightFaceClient, LocalInsightFaceHttpClient>();
        services.AddScoped<IFaceEmbeddingRepository, FaceEmbeddingRepository>();

        services.AddSingleton<AmazonRekognitionClient>(_ =>
        {
            return new AmazonRekognitionClient(RegionEndpoint.EUNorth1);
        });

        services.AddSingleton<IFaceClient>(sp =>
        {
            var o = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AzureFaceOptions>>().Value;
            var client = new FaceClient(new ApiKeyServiceClientCredentials(o.Key)) { Endpoint = o.Endpoint };
            return client;
        });

        services.AddScoped<LocalInsightFaceProvider>(); // IFaceProvider Local
        services.AddScoped<AzureFaceProvider>();
        services.AddScoped<AwsFaceProvider>();

        services.AddScoped<IFaceProviderFactory, FaceProviderFactory>();
        services.AddScoped(provider =>
        {
            var factory = provider.GetRequiredService<IFaceProviderFactory>();
            return factory.Get(); // UnifiedFaceService в следующем шаге
        });

        return services;
    }
}

public interface IFaceProviderFactory
{
    IFaceProvider Get(Abstractions.FaceProviderKind? kind = null);
}

public sealed class FaceProviderFactory : IFaceProviderFactory
{
    private readonly IServiceProvider _sp;
    private readonly FaceProviderOptions _opts;
    public FaceProviderFactory(IServiceProvider sp, Microsoft.Extensions.Options.IOptions<FaceProviderOptions> opts)
    {
        _sp = sp; _opts = opts.Value;
    }

    public IFaceProvider Get(Abstractions.FaceProviderKind? kind = null)
        => (kind ?? _opts.Default) switch
        {
            Abstractions.FaceProviderKind.Local => _sp.GetRequiredService<LocalInsightFaceProvider>(),
            Abstractions.FaceProviderKind.Azure => _sp.GetRequiredService<AzureFaceProvider>(),
            Abstractions.FaceProviderKind.Aws   => _sp.GetRequiredService<AwsFaceProvider>(),
            _ => _sp.GetRequiredService<LocalInsightFaceProvider>()
        };
}
