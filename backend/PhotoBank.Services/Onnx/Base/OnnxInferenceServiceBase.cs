using System;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace PhotoBank.Services.Onnx.Base;

/// <summary>
/// Base class for ONNX inference services with common functionality
/// Provides session management, initialization, and disposal
/// </summary>
public abstract class OnnxInferenceServiceBase : IDisposable
{
    /// <summary>
    /// ONNX Runtime inference session (thread-safe for concurrent inference)
    /// </summary>
    protected InferenceSession? _session;

    /// <summary>
    /// Indicates whether the service has been disposed
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// Initializes the ONNX session with the specified model path and optional CUDA support
    /// </summary>
    /// <param name="modelPath">Path to the ONNX model file</param>
    /// <param name="useCuda">Enable CUDA GPU acceleration (default: true)</param>
    /// <param name="cudaDeviceId">CUDA device ID to use (default: 0)</param>
    /// <exception cref="ArgumentException">Thrown when modelPath is null or empty</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when model file doesn't exist</exception>
    protected void InitializeSession(string modelPath, bool useCuda = true, int cudaDeviceId = 0)
    {
        _session = OnnxSessionFactory.CreateSession(modelPath, useCuda, cudaDeviceId);
    }

    /// <summary>
    /// Executes ONNX inference with a single tensor input.
    /// Properly manages resources using DisposableNamedOnnxValue.
    /// </summary>
    /// <param name="inputName">Name of the input tensor in the model</param>
    /// <param name="tensor">Input tensor data</param>
    /// <returns>Output tensor as float array</returns>
    /// <exception cref="InvalidOperationException">Thrown when session is not initialized</exception>
    protected float[] ExecuteInference(string inputName, DenseTensor<float> tensor)
    {
        if (_session == null)
            throw new InvalidOperationException("ONNX session is not initialized. Call InitializeSession first.");

        return OnnxInferenceHelper.RunInference(_session, inputName, tensor);
    }

    /// <summary>
    /// Disposes the ONNX session and releases resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose pattern implementation
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _session?.Dispose();
                _session = null;
            }

            _disposed = true;
        }
    }
}
