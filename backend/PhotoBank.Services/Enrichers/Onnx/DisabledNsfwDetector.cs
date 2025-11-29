using System;
using System.Collections.Generic;
using ImageMagick;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// No-op NSFW detector used when the ONNX model is unavailable.
/// Keeps the enricher resolvable while preventing pipeline failures.
/// </summary>
public class DisabledNsfwDetector : INsfwDetector
{
    public NsfwDetectionResult Detect(IMagickImage<byte> image)
    {
        return new NsfwDetectionResult
        {
            IsNsfw = false,
            NsfwConfidence = 0,
            IsRacy = false,
            RacyConfidence = 0,
            Scores = new Dictionary<string, float>()
            {
                { "drawings", 0 },
                { "hentai", 0 },
                { "neutral", 1 },
                { "porn", 0 },
                { "sexy", 0 }
            }
        };
    }

    public void Dispose()
    {
    }
}
