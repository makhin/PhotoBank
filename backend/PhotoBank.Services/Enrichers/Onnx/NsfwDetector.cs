using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// NSFW detector using ONNX Runtime and MobileNet model
/// Thread-safe for concurrent use
/// </summary>
public interface INsfwDetector : IDisposable
{
    NsfwDetectionResult Detect(byte[] imageData);
}

public class NsfwDetector : INsfwDetector
{
    private readonly InferenceSession _session;
    private readonly NsfwOnnxOptions _options;
    private const int ImageSize = 224;

    // NSFW model classes: [drawings, hentai, neutral, porn, sexy]
    private static readonly string[] Classes = { "drawings", "hentai", "neutral", "porn", "sexy" };

    public NsfwDetector(IOptions<NsfwOnnxOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.ModelPath))
            throw new ArgumentException("Model path cannot be empty", nameof(options));

        if (!System.IO.File.Exists(_options.ModelPath))
            throw new System.IO.FileNotFoundException($"NSFW model file not found: {_options.ModelPath}");

        var sessionOptions = new SessionOptions();
        // Uncomment for GPU support (requires Microsoft.ML.OnnxRuntime.Gpu)
        // sessionOptions.AppendExecutionProvider_CUDA(0);

        _session = new InferenceSession(_options.ModelPath, sessionOptions);
    }

    public NsfwDetectionResult Detect(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
            throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));

        // Load and preprocess image
        using var image = Image.Load<Rgb24>(imageData);
        image.Mutate(x => x.Resize(ImageSize, ImageSize));

        // Convert to tensor [1, 224, 224, 3] (NHWC format for TensorFlow models)
        var tensor = new DenseTensor<float>(new[] { 1, ImageSize, ImageSize, 3 });

        for (int y = 0; y < ImageSize; y++)
        {
            for (int x = 0; x < ImageSize; x++)
            {
                var pixel = image[x, y];
                // Normalization for MobileNet: (pixel / 255.0 - 0.5) * 2
                tensor[0, y, x, 0] = (pixel.R / 255f - 0.5f) * 2f; // R
                tensor[0, y, x, 1] = (pixel.G / 255f - 0.5f) * 2f; // G
                tensor[0, y, x, 2] = (pixel.B / 255f - 0.5f) * 2f; // B
            }
        }

        // Inference
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", tensor)
        };

        using var results = _session.Run(inputs);
        var output = results.First().AsEnumerable<float>().ToArray();

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

    public void Dispose()
    {
        _session?.Dispose();
    }
}
