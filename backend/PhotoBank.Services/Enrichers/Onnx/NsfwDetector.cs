using System;
using System.Collections.Generic;
using System.IO;
using ImageMagick;
using PhotoBank.NudeNetApiClient;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// NSFW detector interface
/// </summary>
public interface INsfwDetector : IDisposable
{
    NsfwDetectionResult Detect(IMagickImage<byte> image);
}

/// <summary>
/// NSFW detector using NudeNet API
/// Thread-safe for concurrent use
/// </summary>
public class NsfwDetector : INsfwDetector
{
    private readonly INudeNetApiClient _client;
    private bool _disposed;

    public NsfwDetector(INudeNetApiClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public NsfwDetectionResult Detect(IMagickImage<byte> image)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        try
        {
            // Convert image to stream
            using var stream = new MemoryStream();
            image.Write(stream, MagickFormat.Jpeg);
            stream.Position = 0;

            // Call NudeNet API
            var apiResult = _client.DetectAsync(stream).GetAwaiter().GetResult();

            // Convert API result to our format
            return new NsfwDetectionResult
            {
                IsNsfw = apiResult.IsNsfw,
                NsfwConfidence = apiResult.NsfwConfidence,
                IsRacy = apiResult.IsRacy,
                RacyConfidence = apiResult.RacyConfidence,
                Scores = apiResult.Scores
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("NSFW detection failed", ex);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}
