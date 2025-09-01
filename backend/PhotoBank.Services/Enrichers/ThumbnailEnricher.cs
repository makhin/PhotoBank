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

    public async Task EnrichAsync(Photo photo, SourceDataDto source, CancellationToken cancellationToken = default)
    {
        if (photo is null) throw new ArgumentNullException(nameof(photo));
        if (source?.PreviewImage is null)
            return;

        using var srcStream = new MemoryStream(source.PreviewImage.ToByteArray());

        using var thumbStream = await RetryHelper.RetryAsync(
            action: async () =>
            {
                srcStream.Position = 0;
                return await _client
                    .GenerateThumbnailInStreamAsync(Width, Height, srcStream, SmartCropping)
                    .ConfigureAwait(false);
            },
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

        await using var ms = new MemoryStream(capacity: 32 * 1024);
        await thumbStream.CopyToAsync(ms).ConfigureAwait(false);
        source.ThumbnailImage = ms.ToArray();
    }

}

