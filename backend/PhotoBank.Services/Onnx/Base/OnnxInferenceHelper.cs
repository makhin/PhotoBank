using System;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace PhotoBank.Services.Onnx.Base;

/// <summary>
/// Helper class for common ONNX inference operations
/// </summary>
public static class OnnxInferenceHelper
{
    /// <summary>
    /// Executes ONNX inference with a single tensor input
    /// </summary>
    /// <param name="session">ONNX inference session (must be thread-safe)</param>
    /// <param name="inputName">Name of the input tensor in the model</param>
    /// <param name="tensor">Input tensor data</param>
    /// <returns>Output tensor as float array</returns>
    /// <exception cref="ArgumentNullException">Thrown when session or tensor is null</exception>
    public static float[] RunInference(
        InferenceSession session,
        string inputName,
        DenseTensor<float> tensor)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        if (tensor == null)
            throw new ArgumentNullException(nameof(tensor));

        if (string.IsNullOrWhiteSpace(inputName))
            throw new ArgumentException("Input name cannot be null or empty", nameof(inputName));

        var input = NamedOnnxValue.CreateFromTensor(inputName, tensor);
        using var results = session.Run(new[] { input });
        return results.First().AsTensor<float>().ToArray();
    }

    /// <summary>
    /// Executes ONNX inference with a single tensor input (using DisposableNamedOnnxValue)
    /// </summary>
    /// <param name="session">ONNX inference session (must be thread-safe)</param>
    /// <param name="inputName">Name of the input tensor in the model</param>
    /// <param name="tensor">Input tensor data</param>
    /// <returns>Output tensor as float array</returns>
    /// <exception cref="ArgumentNullException">Thrown when session or tensor is null</exception>
    public static float[] RunInferenceDisposable(
        InferenceSession session,
        string inputName,
        DenseTensor<float> tensor)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        if (tensor == null)
            throw new ArgumentNullException(nameof(tensor));

        if (string.IsNullOrWhiteSpace(inputName))
            throw new ArgumentException("Input name cannot be null or empty", nameof(inputName));

        var input = DisposableNamedOnnxValue.CreateFromTensor(inputName, tensor);
        using var results = session.Run(new[] { input });
        return results.First().AsEnumerable<float>().ToArray();
    }
}
