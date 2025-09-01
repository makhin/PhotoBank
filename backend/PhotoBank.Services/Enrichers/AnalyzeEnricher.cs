using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;
using ImageMagick;

namespace PhotoBank.Services.Enrichers;

public sealed class AnalyzeEnricher : IEnricher
{
    private readonly IComputerVisionClient _client;

    // 1) Не аллоцируем список на каждый инстанс
    private static readonly IList<VisualFeatureTypes?> s_features = new VisualFeatureTypes?[]
    {
        VisualFeatureTypes.Categories,
        VisualFeatureTypes.Description,
        VisualFeatureTypes.ImageType,
        VisualFeatureTypes.Tags,
        VisualFeatureTypes.Adult,
        VisualFeatureTypes.Color,
        VisualFeatureTypes.Brands,
        VisualFeatureTypes.Objects
    };

    // 2) Не аллоцируем массив зависимостей каждый раз
    private static readonly Type[] s_dependencies = { typeof(PreviewEnricher) };

    public AnalyzeEnricher(IComputerVisionClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public EnricherType EnricherType => EnricherType.Analyze;

    public Type[] Dependencies => s_dependencies;

    public async Task EnrichAsync(Photo photo, SourceDataDto source, CancellationToken cancellationToken = default)
    {
        if (photo is null) throw new ArgumentNullException(nameof(photo));
        if (source is null) throw new ArgumentNullException(nameof(source));

        if (source.PreviewImage is null)
            return;

        if (source.ImageAnalysis != null)
            return;

        // 4) Read-only поток без лишних копий буфера
        using var stream = new MemoryStream(source.PreviewImage.ToByteArray());

        // 5) Простая политика ретраев (на 429/5xx/сетевые)
        source.ImageAnalysis = await RetryHelper.RetryAsync(
            action: async () =>
                await _client
                    .AnalyzeImageInStreamAsync(
                        image: stream,
                        visualFeatures: s_features,
                        details: null,
                        language: null,
                        modelVersion: "latest")
                    .ConfigureAwait(false),
            attempts: 3,
            delay: TimeSpan.FromMilliseconds(300),
            shouldRetry: ex => ex switch
            {
                ComputerVisionErrorResponseException { Response: { StatusCode: var code } }
                    when code is HttpStatusCode.TooManyRequests
                     or HttpStatusCode.BadGateway
                     or HttpStatusCode.ServiceUnavailable
                     or HttpStatusCode.GatewayTimeout => true,
                HttpRequestException => true,
                _ => false
            }).ConfigureAwait(false);
    }
}
