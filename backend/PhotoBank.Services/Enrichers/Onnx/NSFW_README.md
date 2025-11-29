# NSFW Content Detection with NudeNet ONNX

This enricher uses NudeNet, a YOLOv8-based local ONNX model for detecting nudity and body parts in images.

## Features

- **Local processing**: No external API calls required
- **Object detection**: Detects specific body parts with bounding boxes
- **High accuracy**: YOLOv8-based model with precise localization
- **18 detection classes**: Covered and exposed body parts
- **Configurable thresholds**: Customize detection sensitivity
- **Thread-safe**: Can process multiple images concurrently
- **GPU accelerated**: CUDA support for fast inference

## Model

The enricher uses NudeNet models based on YOLOv8 architecture, which detect 18 different classes:

### Explicit NSFW Classes
- **FEMALE_GENITALIA_EXPOSED**: Exposed female genitalia
- **MALE_GENITALIA_EXPOSED**: Exposed male genitalia
- **FEMALE_BREAST_EXPOSED**: Exposed female breasts
- **ANUS_EXPOSED**: Exposed anus

### Racy/Suggestive Classes
- **BUTTOCKS_EXPOSED**: Exposed buttocks
- **BELLY_EXPOSED**: Exposed belly/stomach
- **ARMPITS_EXPOSED**: Exposed armpits
- **MALE_BREAST_EXPOSED**: Exposed male chest

### Covered Body Parts
- **FEMALE_GENITALIA_COVERED**: Covered female genitalia
- **FEMALE_BREAST_COVERED**: Covered female breasts
- **BUTTOCKS_COVERED**: Covered buttocks
- **BELLY_COVERED**: Covered belly
- **ARMPITS_COVERED**: Covered armpits
- **ANUS_COVERED**: Covered anus
- **FEET_COVERED**: Covered feet
- **FEET_EXPOSED**: Exposed feet

### Face Detection
- **FACE_FEMALE**: Female face
- **FACE_MALE**: Male face

## Setup

### 1. Download the Model

Download NudeNet ONNX models from the official GitHub releases:

**Option 1: 320n model (recommended for speed)**
- Model: `320n.onnx`
- Resolution: 320x320
- Download: [320n.onnx](https://github.com/notAI-tech/NudeNet/releases/download/v3.4-weights/320n.onnx)
- Size: ~6 MB
- Architecture: YOLOv8n
- Best for: Fast inference, good accuracy

**Option 2: 640m model (better accuracy)**
- Model: `640m.onnx`
- Resolution: 640x640
- Download: [640m.onnx](https://github.com/notAI-tech/NudeNet/releases/download/v3.4-weights/640m.onnx)
- Size: ~25 MB
- Architecture: YOLOv8m
- Best for: Higher accuracy, slightly slower

Place the model in your models directory (e.g., `c:\Photobank\models\320n.onnx`)

### 2. Configure appsettings.json

Add the following configuration to your `appsettings.json`:

```json
{
  "NudeNetOnnx": {
    "Enabled": true,
    "ModelPath": "c:\\Photobank\\models\\320n.onnx",
    "InputResolution": 320,
    "ConfidenceThreshold": 0.5,
    "NmsThreshold": 0.45,
    "ExplicitThreshold": 0.6,
    "RacyThreshold": 0.5
  }
}
```

### 3. Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `Enabled` | Enable/disable NudeNet detection | `true` |
| `ModelPath` | Path to the ONNX model file | Required |
| `InputResolution` | Model input resolution (320 or 640) | `320` |
| `ConfidenceThreshold` | Minimum confidence for any detection | `0.5` |
| `NmsThreshold` | Non-Maximum Suppression threshold | `0.45` |
| `ExplicitThreshold` | Threshold for explicit NSFW content | `0.6` |
| `RacyThreshold` | Threshold for racy/suggestive content | `0.5` |

### 4. Install Dependencies

The required NuGet packages are already included in the project:
- `Microsoft.ML.OnnxRuntime.Gpu` (CUDA GPU support)
- `ImageMagick` (image processing)
- `System.Numerics.Tensors`

## Usage

Once configured, the enricher will automatically run during photo ingestion. It will:

1. Load the preview image
2. Resize with letterboxing (preserving aspect ratio)
3. Normalize pixel values to [0, 1] range
4. Run YOLOv8 inference using ONNX Runtime with CUDA
5. Apply Non-Maximum Suppression to filter overlapping detections
6. Classify results as NSFW or Racy based on detected body parts
7. Update the photo record with:
   - `IsAdultContent`: true if explicit nudity detected (genitalia, breasts, anus exposed)
   - `AdultScore`: maximum confidence score among explicit detections
   - `IsRacyContent`: true if suggestive content detected (buttocks, belly exposed)
   - `RacyScore`: maximum confidence score among racy detections

## Detection Logic

- **NSFW**: Image contains at least one explicit class (FEMALE_GENITALIA_EXPOSED, MALE_GENITALIA_EXPOSED, FEMALE_BREAST_EXPOSED, ANUS_EXPOSED) with confidence ≥ ExplicitThreshold
- **Racy**: Image contains at least one racy class (BUTTOCKS_EXPOSED, BELLY_EXPOSED, ARMPITS_EXPOSED, MALE_BREAST_EXPOSED) with confidence ≥ RacyThreshold and is NOT already classified as NSFW

## Performance

**320n model (320x320):**
- **Model size**: ~6 MB
- **Inference time**: ~10-20ms per image (GPU), ~30-50ms (CPU)
- **Predictions**: ~2,100 anchor boxes

**640m model (640x640):**
- **Model size**: ~25 MB
- **Inference time**: ~20-40ms per image (GPU), ~80-150ms (CPU)
- **Predictions**: ~8,400 anchor boxes

**General:**
- **Memory**: Minimal overhead with singleton detector
- **Thread safety**: Full concurrent processing support
- **GPU acceleration**: CUDA support for faster inference

## GPU Acceleration

GPU acceleration with CUDA is **enabled by default** in NudeNet detector:

1. Install CUDA 11.x or 12.x and cuDNN (see [ONNX Runtime GPU requirements](https://onnxruntime.ai/docs/execution-providers/CUDA-ExecutionProvider.html))
2. Ensure `Microsoft.ML.OnnxRuntime.Gpu` NuGet package is installed
3. The detector automatically uses CUDA device 0

GPU acceleration provides 3-5x speedup compared to CPU inference.

## Fallback Behavior

If the NudeNet ONNX model fails to initialize (missing model, ONNX Runtime errors, etc.):
- The `AdultEnricher` will be registered but will use `DisabledNudeNetDetector`
- Adult content detection will be disabled for that session
- A warning will be logged to the console

## Troubleshooting

### Model file not found
- Verify the `ModelPath` in `appsettings.json`
- Ensure the file exists and is readable
- Use absolute paths for reliability
- Check that you downloaded the correct model (320n.onnx or 640m.onnx)

### ONNX Runtime initialization failed
- Install Visual C++ Redistributable (Windows)
- Check ONNX Runtime compatibility with your platform
- For GPU: Install CUDA 11.x/12.x and cuDNN
- Try CPU-only version by using `Microsoft.ML.OnnxRuntime` package

### Poor detection accuracy
- Try the 640m model for better accuracy (at the cost of speed)
- Adjust threshold values in configuration:
  - Lower `ConfidenceThreshold` for more detections
  - Lower `ExplicitThreshold` for more sensitive NSFW classification
  - Lower `RacyThreshold` for more sensitive racy classification
- Check that `InputResolution` matches your model (320 for 320n.onnx, 640 for 640m.onnx)

### Input resolution mismatch
- Ensure `InputResolution` configuration matches the model:
  - 320n.onnx → `"InputResolution": 320`
  - 640m.onnx → `"InputResolution": 640`

## Example Output

```
NudeNet ONNX model validated: c:\Photobank\models\320n.onnx
Input resolution: 320x320
NudeNet ONNX enricher initialized successfully with CUDA GPU acceleration.
NudeNet detection completed for photo 123: IsNsfw=True (confidence=0.89), IsRacy=False (confidence=0.45), Detections=3
NudeNet detections for photo 123: FEMALE_BREAST_EXPOSED=2, FACE_FEMALE=1
```

## References

- [NudeNet GitHub](https://github.com/notAI-tech/NudeNet)
- [NudeNet PyPI Package](https://pypi.org/project/nudenet/)
- [ONNX Runtime Documentation](https://onnxruntime.ai/)
- [YOLOv8 Documentation](https://docs.ultralytics.com/models/yolov8/)
