# NudeNet NSFW Detection Service

Python microservice for NSFW content detection using [NudeNet](https://github.com/notAI-tech/NudeNet).

## Features

- **Fast Detection**: Uses NudeNet's YOLOv8-based model (320x320 default)
- **18 Detection Classes**: Detects various body parts and clothing states
- **Two-Level Classification**:
  - NSFW (explicit content): exposed genitalia, breasts, anus
  - Racy (suggestive content): covered sensitive areas, exposed buttocks
- **RESTful API**: FastAPI-based service with automatic documentation

## API Endpoints

### Health Check
```
GET /health
```

### Detect NSFW Content
```
POST /detect
Content-Type: multipart/form-data

Parameters:
  - file: Image file (JPEG, PNG, etc.)

Response:
{
  "is_nsfw": bool,
  "nsfw_confidence": float (0-1),
  "is_racy": bool,
  "racy_confidence": float (0-1),
  "scores": {
    "FEMALE_GENITALIA_EXPOSED": 0.95,
    ...
  },
  "detections": [
    {
      "class": "FEMALE_GENITALIA_EXPOSED",
      "score": 0.95,
      "box": [x, y, width, height]
    }
  ]
}
```

## Detection Classes

### Explicit Classes (NSFW)
- `FEMALE_GENITALIA_EXPOSED`
- `MALE_GENITALIA_EXPOSED`
- `ANUS_EXPOSED`
- `FEMALE_BREAST_EXPOSED`

### Racy Classes
- `FEMALE_BREAST_COVERED`
- `BUTTOCKS_EXPOSED`
- `FEMALE_GENITALIA_COVERED`

### Safe Classes
- `FACE_FEMALE` / `FACE_MALE`
- `BUTTOCKS_COVERED`
- `FEET_EXPOSED` / `FEET_COVERED`
- `ARMPITS_EXPOSED` / `ARMPITS_COVERED`
- `BELLY_EXPOSED` / `BELLY_COVERED`
- `MALE_BREAST_EXPOSED`

## Running Locally

### Prerequisites
- Python 3.11+
- pip

### Installation
```bash
cd nudenet
pip install -r requirements.txt
```

### Run
```bash
uvicorn app.main:app --host 0.0.0.0 --port 5556
```

The service will be available at `http://localhost:5556`

Interactive API docs: `http://localhost:5556/docs`

## Docker

### Build
```bash
docker build -t photobank-nudenet:latest -f nudenet/Dockerfile nudenet
```

### Run
```bash
docker run -p 5556:5556 photobank-nudenet:latest
```

## Integration with PhotoBank

The NudeNet service is integrated with PhotoBank through:

1. **C# Client**: `PhotoBank.NudeNet.Client` - HTTP client for the API
2. **NSFW Detector**: `PhotoBank.Services.Enrichers.Onnx.NsfwDetector` - Uses the client
3. **Adult Enricher**: `PhotoBank.Services.Enrichers.AdultEnricher` - Enrichment pipeline

### Configuration

Add to `appsettings.json`:
```json
{
  "NudeNet": {
    "Enabled": true,
    "BaseUrl": "http://localhost:5556"
  }
}
```

## Model Information

- **Default Model**: YOLOv8n 320x320 (fast, balanced)
- **Alternative**: YOLOv8m 640x640 (more accurate, slower)
- **Model Source**: [NudeNet](https://github.com/notAI-tech/NudeNet)
- **Model downloads automatically on first run**

## Performance

- **Cold start**: ~5-10 seconds (model loading)
- **Inference time**: ~50-100ms per image (320x320 model)
- **Memory usage**: ~500MB (includes model and runtime)
