using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.DbContext.Models;
using PhotoBank.Services.Models;

namespace PhotoBank.Services.Enrichers;

public sealed class ThumbnailEnricher : IEnricher
{
    private readonly IComputerVisionClient _client;

    public EnricherType EnricherType => EnricherType.Thumbnail;

    private static readonly Type[] s_dependencies = { typeof(PreviewEnricher) };
    public Type[] Dependencies => s_dependencies;

    private const int Width = 50;
    private const int Height = 50;
    private const bool SmartCropping = true;

    public ThumbnailEnricher(IComputerVisionClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task EnrichAsync(Photo photo, SourceDataDto _, CancellationToken cancellationToken = default)
    {
        if (photo is null) throw new ArgumentNullException(nameof(photo));
        if (photo.PreviewImage is null || photo.PreviewImage.Length == 0)
            return; // нечего генерировать

        if (photo.Thumbnail is { Length: > 0 })
            return; // уже есть

        // Источник: read-only MemoryStream поверх массива без копий
        using var srcStream = new MemoryStream(
            buffer: photo.PreviewImage,
            index: 0,
            count: photo.PreviewImage.Length,
            writable: false,
            publiclyVisible: true);

        // Ретраи + перемотка стрима перед каждой попыткой
        using var thumbStream = await RetryAsync(
            action: async () =>
            {
                srcStream.Position = 0; // важно при ретраях
                return await _client
                    .GenerateThumbnailInStreamAsync(Width, Height, srcStream, SmartCropping)
                    .ConfigureAwait(false);
            },
            attempts: 3,
            initialDelayMs: 300).ConfigureAwait(false);

        // Копируем в byte[]; thumbnail маленький — стартуем с 32 КБ
        await using var ms = new MemoryStream(capacity: 32 * 1024);
        await thumbStream.CopyToAsync(ms).ConfigureAwait(false);
        photo.Thumbnail = ms.ToArray();
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
            catch (ComputerVisionErrorResponseException ex)
                when (IsRetryable(ex.Response?.StatusCode) && tryNo < attempts)
            {
                // fallthrough to delay
            }
            catch (HttpRequestException)
                when (tryNo < attempts)
            {
                // fallthrough to delay
            }

              await Task.Delay(delay).ConfigureAwait(false);
              delay = Math.Min(delay * 2, 4000); // экспоненциально до 4с
          }

        static bool IsRetryable(HttpStatusCode? code) =>
            code is HttpStatusCode.TooManyRequests     // 429
             or HttpStatusCode.BadGateway              // 502
             or HttpStatusCode.ServiceUnavailable      // 503
             or HttpStatusCode.GatewayTimeout;         // 504
    }
}