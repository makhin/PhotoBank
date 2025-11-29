"""
FastAPI service for NSFW detection using NudeNet
"""
import logging
from fastapi import FastAPI, UploadFile, File
from .detection_service import detect_nsfw

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)

app = FastAPI(
    title="NudeNet NSFW Detection API",
    version="1.0",
    description="NSFW content detection service using NudeNet"
)


@app.get("/health")
async def health():
    """Health check endpoint"""
    return {"status": "ok"}


@app.post("/detect")
async def detect(file: UploadFile = File(...)):
    """
    Detect NSFW content in an image.

    Analyzes the provided image and returns detection results including:
    - Whether the image contains explicit (NSFW) content
    - Whether the image contains suggestive (racy) content
    - Confidence scores for both categories
    - Detailed detection results for all identified objects

    Args:
        file: Image file to analyze

    Returns:
        JSON response with detection results:
        {
            "is_nsfw": bool,
            "nsfw_confidence": float (0-1),
            "is_racy": bool,
            "racy_confidence": float (0-1),
            "scores": dict of class names to scores,
            "detections": list of detected objects with bounding boxes
        }
    """
    return detect_nsfw(file)
