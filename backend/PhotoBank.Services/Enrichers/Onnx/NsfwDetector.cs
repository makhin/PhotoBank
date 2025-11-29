using System;
using System.Collections.Generic;
using System.Linq;
using ImageMagick;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoBank.Services.Onnx.Base;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// NSFW detector using ONNX Runtime and MobileNet model
/// Thread-safe for concurrent use
/// </summary>
public interface INsfwDetector : IDisposable
{
    NsfwDetectionResult Detect(IMagickImage<byte> image);
}

public class NsfwDetector : OnnxInferenceServiceBase, INsfwDetector
{
    private readonly NsfwOnnxOptions _options;
    private const int ImageSize = 224;

    // NSFW model classes: [drawings, hentai, neutral, porn, sexy]
    private static readonly string[] Classes = { "drawings", "hentai", "neutral", "porn", "sexy" };

    public NsfwDetector(IOptions<NsfwOnnxOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // Initialize ONNX session with CUDA GPU acceleration
        // Validation is handled by the base class (OnnxSessionFactory)
        InitializeSession(_options.ModelPath, useCuda: true, cudaDeviceId: 0);
    }

    public NsfwDetectionResult Detect(IMagickImage<byte> image)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        // Resize image to 224x224
        using var resized = image.Clone();
        resized.Resize(ImageSize, ImageSize);
        resized.ColorSpace = ColorSpace.sRGB;

        // Convert to tensor [1, 224, 224, 3] (NHWC format for TensorFlow models)
        var tensor = new DenseTensor<float>(new[] { 1, ImageSize, ImageSize, 3 });

        // Get pixels from ImageMagick
        var pixels = resized.GetPixels();

        for (int y = 0; y < ImageSize; y++)
        {
            for (int x = 0; x < ImageSize; x++)
            {
                var pixel = pixels.GetPixel(x, y);
                // Normalization for MobileNet: (pixel / 255.0 - 0.5) * 2
                tensor[0, y, x, 0] = (pixel.GetChannel(0) / 255f - 0.5f) * 2f; // R
                tensor[0, y, x, 1] = (pixel.GetChannel(1) / 255f - 0.5f) * 2f; // G
                tensor[0, y, x, 2] = (pixel.GetChannel(2) / 255f - 0.5f) * 2f; // B
            }
        }

        // Inference using base class method (with proper resource management)
        var output = ExecuteInference("input", tensor);

        // Model returns [drawings, hentai, neutral, porn, sexy]
        var scores = new Dictionary<string, float>();
        for (int i = 0; i < Classes.Length; i++)
        {
            scores[Classes[i]] = output[i];
        }

        // Calculate NSFW detection
        var pornScore = scores["porn"];
        var sexyScore = scores["sexy"];
        var hentaiScore = scores["hentai"];

        // Use max of porn, sexy (weighted), and hentai as overall NSFW score
        var nsfwScore = Math.Max(pornScore, Math.Max(sexyScore * 0.8f, hentaiScore));
        var isNsfw = pornScore > _options.PornThreshold ||
                     sexyScore > _options.SexyThreshold ||
                     hentaiScore > _options.HentaiThreshold;

        // Racy detection: sexy content that's not explicitly NSFW
        var isRacy = sexyScore > _options.RacyMinThreshold &&
                     sexyScore <= _options.RacyMaxThreshold &&
                     pornScore < _options.PornThreshold;

        return new NsfwDetectionResult
        {
            IsNsfw = isNsfw,
            NsfwConfidence = nsfwScore, // Raw probability (0-1) of NSFW content
            IsRacy = isRacy,
            RacyConfidence = sexyScore, // Raw probability (0-1) of racy content
            Scores = scores
        };
    }

    // Dispose is inherited from OnnxInferenceServiceBase
}
