using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers;

public sealed class AnalyzeEnricher : IEnricher
{
    private readonly IComputerVisionClient _client;

    // 1) Не аллоцируем список на каждый инстанс
    private static readonly IReadOnlyList<VisualFeatureTypes?> s_features = new VisualFeatureTypes?[]
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

    public async Task EnrichAsync(Photo photo, SourceDataDto source)
    {
        if (photo is null) throw new ArgumentNullException(nameof(photo));
        if (source is null) throw new ArgumentNullException(nameof(source));

        // 3) Быстрые проверки — нет превью или уже обогащено
        if (photo.PreviewImage is null || photo.PreviewImage.Length == 0)
            return;

        if (source.ImageAnalysis != null)
            return;

        // 4) Read-only поток без лишних копий буфера
        using var stream = new MemoryStream(
            buffer: photo.PreviewImage,
            index: 0,
            count: photo.PreviewImage.Length,
            writable: false,
            publiclyVisible: true);

        // 5) Простая политика ретраев (на 429/5xx/сетевые)
        source.ImageAnalysis = await RetryAsync(
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
            initialDelayMs: 300).ConfigureAwait(false);
    }

    private static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        int attempts,
        int initialDelayMs)
    {
        var delay = initialDelayMs;

        for (var tryNo = 1; ; tryNo++)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (ComputerVisionErrorResponseException ex) when (IsRetryable(ex.Response?.StatusCode))
            {
                if (tryNo >= attempts) throw;
            }
            catch (HttpRequestException) when (tryNo < attempts)
            {
                // сетевые/транзиентные
            }

            await Task.Delay(delay).ConfigureAwait(false);
            delay = Math.Min(delay * 2, 4000); // экспоненциально до 4с
        }

        static bool IsRetryable(HttpStatusCode? code) =>
            code is HttpStatusCode.TooManyRequests // 429
             or HttpStatusCode.BadGateway          // 502
             or HttpStatusCode.ServiceUnavailable  // 503
             or HttpStatusCode.GatewayTimeout;     // 504
    }
}