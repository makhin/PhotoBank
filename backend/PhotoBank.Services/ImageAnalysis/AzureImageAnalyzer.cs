using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace PhotoBank.Services.ImageAnalysis;

public sealed class AzureImageAnalyzer : IImageAnalyzer
{
    private readonly IComputerVisionClient _client;

    private static readonly IList<VisualFeatureTypes?> Features =
    [
        VisualFeatureTypes.Categories,
        VisualFeatureTypes.Description,
        VisualFeatureTypes.ImageType,
        VisualFeatureTypes.Tags,
        VisualFeatureTypes.Adult,
        VisualFeatureTypes.Color,
        VisualFeatureTypes.Brands,
        VisualFeatureTypes.Objects
    ];

    public AzureImageAnalyzer(IComputerVisionClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public ImageAnalyzerKind Kind => ImageAnalyzerKind.Azure;

    public async Task<ImageAnalysisResult> AnalyzeAsync(Stream image, CancellationToken ct = default)
    {
        // Buffer the image data once to allow stream rewind on retry
        byte[] imageData;
        using (var ms = new MemoryStream())
        {
            await image.CopyToAsync(ms, ct).ConfigureAwait(false);
            imageData = ms.ToArray();
        }

        var azureResult = await RetryHelper.RetryAsync(
            action: async () =>
            {
                using var retryStream = new MemoryStream(imageData);
                return await _client
                    .AnalyzeImageInStreamAsync(
                        image: retryStream,
                        visualFeatures: Features,
                        details: null,
                        language: null,
                        modelVersion: "latest",
                        cancellationToken: ct)
                    .ConfigureAwait(false);
            },
            attempts: 3,
            delay: TimeSpan.FromMilliseconds(300),
            shouldRetry: ex => ex switch
            {
                ComputerVisionErrorResponseException { Response.StatusCode: var code }
                    when code is HttpStatusCode.TooManyRequests
                        or HttpStatusCode.BadGateway
                        or HttpStatusCode.ServiceUnavailable
                        or HttpStatusCode.GatewayTimeout => true,
                HttpRequestException => true,
                _ => false
            }).ConfigureAwait(false);

        return MapToResult(azureResult);
    }

    private static ImageAnalysisResult MapToResult(Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ImageAnalysis azure)
    {
        return new ImageAnalysisResult
        {
            Description = azure.Description != null ? new ImageDescription
            {
                Captions = azure.Description.Captions?
                    .Select(c => new ImageCaption { Text = c.Text, Confidence = c.Confidence })
                    .ToList() ?? []
            } : null,

            Tags = azure.Tags?
                .Select(t => new ImageTag { Name = t.Name, Confidence = t.Confidence })
                .ToList() ?? [],

            Categories = azure.Categories?
                .Select(c => new ImageCategory { Name = c.Name, Score = c.Score })
                .ToList() ?? [],

            Objects = azure.Objects?
                .Select(o => new DetectedObject
                {
                    ObjectProperty = o.ObjectProperty,
                    Confidence = o.Confidence,
                    Rectangle = o.Rectangle != null ? new ObjectRectangle
                    {
                        X = o.Rectangle.X,
                        Y = o.Rectangle.Y,
                        W = o.Rectangle.W,
                        H = o.Rectangle.H
                    } : null
                })
                .ToList() ?? [],

            Adult = azure.Adult != null ? new AdultContent
            {
                IsAdultContent = azure.Adult.IsAdultContent,
                AdultScore = azure.Adult.AdultScore,
                IsRacyContent = azure.Adult.IsRacyContent,
                RacyScore = azure.Adult.RacyScore
            } : null,

            Color = azure.Color != null ? new ColorInfo
            {
                IsBWImg = azure.Color.IsBWImg,
                AccentColor = azure.Color.AccentColor,
                DominantColorBackground = azure.Color.DominantColorBackground,
                DominantColorForeground = azure.Color.DominantColorForeground,
                DominantColors = azure.Color.DominantColors?.ToList() ?? []
            } : null
        };
    }
}
