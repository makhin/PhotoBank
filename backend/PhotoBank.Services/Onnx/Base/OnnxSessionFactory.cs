using System;
using System.IO;
using Microsoft.ML.OnnxRuntime;

namespace PhotoBank.Services.Onnx.Base;

/// <summary>
/// Factory for creating ONNX Runtime inference sessions with optional GPU acceleration
/// </summary>
public static class OnnxSessionFactory
{
    /// <summary>
    /// Creates an ONNX Runtime inference session with optional CUDA GPU acceleration
    /// </summary>
    /// <param name="modelPath">Path to the ONNX model file</param>
    /// <param name="useCuda">Enable CUDA GPU acceleration (default: true)</param>
    /// <param name="cudaDeviceId">CUDA device ID to use (default: 0)</param>
    /// <returns>Configured InferenceSession</returns>
    /// <exception cref="ArgumentException">Thrown when modelPath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when model file doesn't exist</exception>
    public static InferenceSession CreateSession(
        string modelPath,
        bool useCuda = true,
        int cudaDeviceId = 0)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("Model path cannot be null or empty", nameof(modelPath));

        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"ONNX model file not found: {modelPath}", modelPath);

        var sessionOptions = new SessionOptions();

        if (useCuda)
        {
            sessionOptions.AppendExecutionProvider_CUDA(cudaDeviceId);
        }

        return new InferenceSession(modelPath, sessionOptions);
    }

    /// <summary>
    /// Validates that a model file exists and can be loaded without creating a persistent session
    /// </summary>
    /// <param name="modelPath">Path to the ONNX model file</param>
    /// <param name="useCuda">Test with CUDA GPU acceleration (default: false for validation)</param>
    /// <returns>True if model is valid and can be loaded</returns>
    public static bool ValidateModel(string modelPath, bool useCuda = false)
    {
        try
        {
            using var session = CreateSession(modelPath, useCuda);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
