using Microsoft.ML.Data;

namespace PhotoBank.Services.Enrichers.Onnx;

/// <summary>
/// Input data for YOLO model
/// </summary>
public class YoloImageInput
{
    [ColumnName("images")]
    [VectorType(1, 3, 640, 640)]
    public float[]? Image { get; set; }
}

/// <summary>
/// Output data from YOLO model
/// </summary>
public class YoloOutput
{
    [ColumnName("output0")]
    public float[]? Output { get; set; }
}

/// <summary>
/// Represents a detected object with bounding box and class information
/// </summary>
public class DetectedObjectOnnx
{
    public string ClassName { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    /// <summary>
    /// Bounding box in format: [x_min, y_min, x_max, y_max]
    /// </summary>
    public float[] BoundingBox => new[] { X, Y, X + Width, Y + Height };
}

/// <summary>
/// COCO dataset class names (80 classes)
/// </summary>
public static class CocoClassNames
{
    public static readonly string[] Names = new[]
    {
        "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck", "boat", "traffic light",
        "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow",
        "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee",
        "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard",
        "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple",
        "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "couch",
        "potted plant", "bed", "dining table", "toilet", "tv", "laptop", "mouse", "remote", "keyboard", "cell phone",
        "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear",
        "hair drier", "toothbrush"
    };
}
