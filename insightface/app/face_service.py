from .db import SessionLocal, PersonEmbedding
from fastapi import UploadFile
import numpy as np
import insightface
import json
import logging
import cv2

# Setup logger
logger = logging.getLogger("face_service")
logger.setLevel(logging.INFO)

# Initialize InsightFace once at startup
logger.info("Initializing InsightFace model...")
app_insightface = insightface.app.FaceAnalysis()
app_insightface.prepare(ctx_id=0, det_size=(640, 640))
logger.info("InsightFace model initialized with det_size=(640, 640)")

EMOTIONS = ['neutral', 'happy', 'surprise', 'sad', 'anger', 'disgust', 'fear', 'contempt']

def adaptive_scale(img):
    """
    Scale image based on its size to optimize detection performance.
    """
    h, w = img.shape[:2]
    max_dim = max(h, w)
    if max_dim <= 320:
        scale_factor = 1.0
    elif max_dim <= 640:
        scale_factor = 0.75
    elif max_dim <= 1280:
        scale_factor = 0.5
    else:
        scale_factor = 0.25
    logger.info(f"Scaling image from {w}x{h} by factor {scale_factor}")
    return cv2.resize(img, (int(w * scale_factor), int(h * scale_factor)))

def process_image(file: UploadFile):
    logger.info(f"Processing uploaded file: {file.filename}")
    img_bytes = file.file.read()
    import cv2
    np_arr = np.frombuffer(img_bytes, np.uint8)
    img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
    if img is None:
        logger.error("Failed to decode image")
        return {"error": "Invalid image format"}

    logger.info(f"Original image size: {img.shape[1]}x{img.shape[0]}")
    scaled_img = adaptive_scale(img)

    faces = app_insightface.get(scaled_img)
    logger.info(f"Detected {len(faces)} face(s) in the image")

    if not faces:
        return {"error": "No face detected"}
    return faces

def extract_face_data(face):
    """
    Extract face attributes including embedding, age, gender, emotion, and pose.
    """
    emotion_idx = getattr(face, 'emotion', None)
    emotion = EMOTIONS[emotion_idx] if emotion_idx is not None and emotion_idx < len(EMOTIONS) else "unknown"
    return {
        "embedding": face.embedding.tolist(),
        "shape": face.embedding.shape,
        "bbox": face.bbox.tolist(),
        "gender": "male" if face.gender == 1 else "female",
        "age": face.age,
        "emotion": emotion,
        "pose": {
            "yaw": face.pose.yaw,
            "pitch": face.pose.pitch,
            "roll": face.pose.roll
        }
    }

def register_face(person_id: int, file: UploadFile):
    logger.info(f"Registering face for person_id={person_id}")
    faces = process_image(file)
    if isinstance(faces, dict) and "error" in faces:
        logger.error(f"Face registration failed: {faces['error']}")
        return faces  # Return error
    face = faces[0]
    emb_bin = face.embedding.tobytes()
    emb_json = json.dumps(face.embedding.tolist())
    try:
        with SessionLocal() as db:
            embedding = PersonEmbedding(
                person_id=person_id,
                embedding_binary=emb_bin,
                embedding_json=emb_json
            )
            db.add(embedding)
            db.commit()
            logger.info(f"Face embedding registered for person_id={person_id}")
            return {
                "status": "registered",
                "person_id": person_id,
                "details": extract_face_data(face)
            }
    except Exception as e:
        logger.exception("Database error during face registration")
        return {"error": "Database error"}

def recognize_faces(file: UploadFile):
    logger.info("Recognizing faces in uploaded image")
    faces = process_image(file)
    if isinstance(faces, dict) and "error" in faces:
        logger.error(f"Face recognition failed: {faces['error']}")
        return faces  # Return error

    face_data = [extract_face_data(face) for face in faces]
    logger.info(f"Returning data for {len(face_data)} detected face(s)")
    return {"faces": face_data, "count": len(face_data)}
