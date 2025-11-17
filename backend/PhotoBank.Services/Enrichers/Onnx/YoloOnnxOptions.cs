namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// Configuration options for YOLO ONNX model
/// </summary>
public class YoloOnnxOptions
{
    /// <summary>
    /// Path to the ONNX model file (e.g., yolov8n.onnx)
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Minimum confidence threshold for detections (default: 0.5)
    /// </summary>
    public float ConfidenceThreshold { get; set; } = 0.5f;

    /// <summary>
    /// NMS (Non-Maximum Suppression) threshold (default: 0.45)
    /// </summary>
    public float NmsThreshold { get; set; } = 0.45f;

    /// <summary>
    /// Enable or disable ONNX object detection enricher (default: false)
    /// </summary>
    public bool Enabled { get; set; } = false;
}
