# NSFW Content Detection with ONNX

This enricher uses a local ONNX MobileNet model to detect NSFW (Not Safe For Work) content in images.

## Features

- **Local processing**: No external API calls required
- **Fast inference**: MobileNet-based model optimized for speed
- **Multiple classifications**: Detects porn, sexy, hentai, drawings, and neutral content
- **Configurable thresholds**: Customize detection sensitivity
- **Thread-safe**: Can process multiple images concurrently

## Model

The enricher uses the NSFW MobileNet v2 model, which classifies images into 5 categories:
- **drawings**: Cartoon/drawn content
- **hentai**: Anime/manga adult content
- **neutral**: Safe content
- **porn**: Explicit adult content
- **sexy**: Suggestive content

## Setup

### 1. Download the Model

Download the NSFW MobileNet ONNX model:
- Model: `nsfw_mobilenet2.onnx`
- Source: [GantMan/nsfw_model](https://github.com/GantMan/nsfw_model) or [ONNX Model Zoo](https://github.com/onnx/models)
- Place it in your models directory (e.g., `c:\Photobank\models\nsfw_mobilenet2.onnx`)

You can convert the TensorFlow model to ONNX using:
```bash
pip install tf2onnx tensorflow
python -m tf2onnx.convert --saved-model nsfw_mobilenet2_model --output nsfw_mobilenet2.onnx
```

### 2. Configure appsettings.json

Add the following configuration to your `appsettings.json`:

```json
{
  "NsfwOnnx": {
    "Enabled": true,
    "ModelPath": "c:\\Photobank\\models\\nsfw_mobilenet2.onnx",
    "PornThreshold": 0.5,
    "SexyThreshold": 0.7,
    "HentaiThreshold": 0.6,
    "RacyMinThreshold": 0.4,
    "RacyMaxThreshold": 0.7
  }
}
```

### 3. Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `Enabled` | Enable/disable NSFW detection | `true` |
| `ModelPath` | Path to the ONNX model file | Required |
| `PornThreshold` | Minimum score to classify as porn | `0.5` |
| `SexyThreshold` | Minimum score to classify as NSFW sexy content | `0.7` |
| `HentaiThreshold` | Minimum score to classify as hentai | `0.6` |
| `RacyMinThreshold` | Minimum score for racy content | `0.4` |
| `RacyMaxThreshold` | Maximum score for racy content (above this is NSFW) | `0.7` |

### 4. Install Dependencies

The required NuGet packages are already included in the project:
- `Microsoft.ML.OnnxRuntime` (or `.Gpu` for GPU support)
- `SixLabors.ImageSharp`
- `System.Numerics.Tensors`

## Usage

Once configured, the enricher will automatically run during photo ingestion. It will:

1. Load the image
2. Resize it to 224x224 pixels
3. Normalize pixel values for MobileNet
4. Run inference using ONNX Runtime
5. Update the photo record with:
   - `IsAdultContent`: true if classified as NSFW
   - `AdultScore`: confidence score for NSFW detection
   - `IsRacyContent`: true if classified as racy (suggestive but not explicit)
   - `RacyScore`: confidence score for racy detection

## Performance

- **Model size**: ~17 MB
- **Inference time**: ~20-50ms per image (CPU)
- **Memory**: Minimal overhead with singleton detector
- **Thread safety**: Full concurrent processing support

## GPU Acceleration (Optional)

To enable GPU acceleration:

1. Install CUDA and cuDNN (see [ONNX Runtime GPU requirements](https://onnxruntime.ai/docs/execution-providers/CUDA-ExecutionProvider.html))
2. Uncomment the GPU provider line in `NsfwDetector.cs`:
   ```csharp
   sessionOptions.AppendExecutionProvider_CUDA(0);
   ```
3. Ensure `Microsoft.ML.OnnxRuntime.Gpu` is installed

## Fallback Behavior

If the NSFW ONNX model fails to initialize (missing model, ONNX Runtime errors, etc.):
- The `AdultEnricher` will be registered but will use `DisabledNsfwDetector`
- Adult content detection will be disabled for that session
- A warning will be logged to the console

## Troubleshooting

### Model file not found
- Verify the `ModelPath` in `appsettings.json`
- Ensure the file exists and is readable
- Use absolute paths for reliability

### ONNX Runtime initialization failed
- Install Visual C++ Redistributable (Windows)
- Check ONNX Runtime compatibility with your platform
- Try CPU-only version first before enabling GPU

### Poor detection accuracy
- Adjust threshold values in configuration
- Lower thresholds = more sensitive detection
- Higher thresholds = more conservative detection

## Example Output

```
NSFW ONNX enricher initialized successfully.
NSFW detection completed for photo 123: IsNsfw=True (confidence=0.87), IsRacy=False (confidence=0.13)
NSFW scores for photo 123: porn=0.872, sexy=0.098, hentai=0.012, neutral=0.015, drawings=0.003
```

## References

- [NSFW Model GitHub](https://github.com/GantMan/nsfw_model)
- [ONNX Runtime Documentation](https://onnxruntime.ai/)
- [MobileNet v2 Paper](https://arxiv.org/abs/1801.04381)
