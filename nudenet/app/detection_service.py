"""
NSFW detection service using NudeNet
"""
import io
import logging
from typing import Dict, List
import cv2
import numpy as np
from fastapi import UploadFile
from nudenet import NudeDetector

logger = logging.getLogger(__name__)

# Initialize NudeDetector globally (loads model once)
# Using default 320n model for balance between speed and accuracy
detector = NudeDetector()

# NSFW class mappings
# NudeNet detects 18 classes, we categorize them into different levels
EXPLICIT_CLASSES = {
    'FEMALE_GENITALIA_EXPOSED',
    'MALE_GENITALIA_EXPOSED',
    'ANUS_EXPOSED',
    'FEMALE_BREAST_EXPOSED',
}

RACY_CLASSES = {
    'FEMALE_BREAST_COVERED',
    'BUTTOCKS_EXPOSED',
    'FEMALE_GENITALIA_COVERED',
}

SAFE_CLASSES = {
    'FACE_FEMALE',
    'FACE_MALE',
    'BUTTOCKS_COVERED',
    'FEET_EXPOSED',
    'FEET_COVERED',
    'ARMPITS_EXPOSED',
    'ARMPITS_COVERED',
    'BELLY_EXPOSED',
    'BELLY_COVERED',
    'MALE_BREAST_EXPOSED',
}


def detect_nsfw(file: UploadFile) -> Dict:
    """
    Detect NSFW content in an image using NudeNet.

    Args:
        file: Uploaded image file

    Returns:
        Dictionary with detection results including:
        - is_nsfw: Boolean indicating if image contains explicit content
        - nsfw_confidence: Confidence score for NSFW content (0-1)
        - is_racy: Boolean indicating if image contains suggestive content
        - racy_confidence: Confidence score for racy content (0-1)
        - detections: List of all detected classes with scores
    """
    try:
        # Read image from upload
        image_bytes = file.file.read()
        nparr = np.frombuffer(image_bytes, np.uint8)
        image = cv2.imdecode(nparr, cv2.IMREAD_COLOR)

        if image is None:
            raise ValueError("Failed to decode image")

        # Run detection
        # Returns list of dicts: [{'class': 'FEMALE_BREAST_EXPOSED', 'score': 0.95, 'box': [x, y, w, h]}, ...]
        detections = detector.detect(image)

        # Calculate scores
        explicit_scores = []
        racy_scores = []
        all_detections = []

        for detection in detections:
            class_name = detection['class']
            score = detection['score']

            all_detections.append({
                'class': class_name,
                'score': float(score),
                'box': detection['box']
            })

            if class_name in EXPLICIT_CLASSES:
                explicit_scores.append(score)
            elif class_name in RACY_CLASSES:
                racy_scores.append(score)

        # Calculate overall scores
        # NSFW: max of explicit detections
        nsfw_confidence = max(explicit_scores) if explicit_scores else 0.0
        is_nsfw = nsfw_confidence > 0.5

        # Racy: max of racy detections, but only if not already NSFW
        racy_confidence = max(racy_scores) if racy_scores else 0.0
        is_racy = racy_confidence > 0.4 and not is_nsfw

        # Build class scores dict for compatibility with old API
        scores = {}
        for detection in all_detections:
            class_name = detection['class']
            score = detection['score']
            # Keep the highest score if class appears multiple times
            if class_name not in scores or score > scores[class_name]:
                scores[class_name] = score

        result = {
            'is_nsfw': is_nsfw,
            'nsfw_confidence': float(nsfw_confidence),
            'is_racy': is_racy,
            'racy_confidence': float(racy_confidence),
            'scores': scores,
            'detections': all_detections
        }

        logger.info(
            f"Detection completed: is_nsfw={is_nsfw} ({nsfw_confidence:.2f}), "
            f"is_racy={is_racy} ({racy_confidence:.2f}), "
            f"detected {len(all_detections)} objects"
        )

        return result

    except Exception as e:
        logger.error(f"Error during NSFW detection: {e}")
        raise
    finally:
        # Reset file pointer for potential reuse
        file.file.seek(0)
