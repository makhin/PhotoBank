from .db import SessionLocal, PersonEmbedding
from fastapi import UploadFile
import numpy as np
import insightface
import json
import logging
import cv2
import os

# Setup logger
logger = logging.getLogger("face_service")
logger.setLevel(logging.INFO)

# Initialize InsightFace with antelopev2 model pack (includes glint360k recognition model)
# NOTE: antelopev2 uses ResNet100@Glint360K (highest quality available in standard packs)
# For ResNet50@Glint360K, you can download the specific ONNX model and load it via:
# recognition_model = insightface.model_zoo.get_model('path/to/glint360k_r50.onnx')
logger.info("Initializing InsightFace ArcFace model (antelopev2 with glint360k)...")
app_insightface = insightface.app.FaceAnalysis(
    name='antelopev2',  # Uses ResNet100@Glint360K for recognition
    providers=['CUDAExecutionProvider', 'CPUExecutionProvider']
)
app_insightface.prepare(ctx_id=0, det_size=(640, 640))

# Extract the recognition model for direct use with cropped faces
recognition_model = None
for model in app_insightface.models.values():
    if hasattr(model, 'get') and 'rec' in str(type(model)).lower():
        recognition_model = model
        logger.info(f"Extracted recognition model: {type(model).__name__}")
        break

if recognition_model is None:
    logger.warning("Could not extract recognition model, will use FaceAnalysis")

logger.info("InsightFace model initialized successfully")

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

def preprocess_face_image(img):
    """
    Preprocess and resize face image to 112x112 for ArcFace model.
    Handles both BGR and RGB images.
    """
    # Resize to 112x112 as required by ArcFace models
    if img.shape[:2] != (112, 112):
        img = cv2.resize(img, (112, 112), interpolation=cv2.INTER_LINEAR)

    # Ensure image is in RGB format for InsightFace
    if len(img.shape) == 2:  # Grayscale
        img = cv2.cvtColor(img, cv2.COLOR_GRAY2RGB)
    elif img.shape[2] == 4:  # RGBA
        img = cv2.cvtColor(img, cv2.COLOR_RGBA2RGB)

    return img

def get_embedding_from_cropped_face(img):
    """
    Extract face embedding from a pre-cropped face image.

    Args:
        img: numpy array of face image (will be resized to 112x112)

    Returns:
        numpy array of face embedding (512-dimensional vector)
    """
    # Preprocess the image
    processed_img = preprocess_face_image(img)

    # Use the recognition model directly
    # The model expects RGB image normalized and in the correct format
    embedding = recognition_model.get_feat(processed_img)

    return embedding

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

def register_face(face_id: int, file: UploadFile):
    logger.info(f"Registering face embedding for face_id={face_id}")
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
                face_id=face_id,
                embedding_binary=emb_bin,
                embedding_json=emb_json
            )
            db.add(embedding)
            db.commit()
            logger.info(f"Face embedding registered for face_id={face_id}")
            return {
                "status": "registered",
                "face_id": face_id,
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

def embed_cropped_face(file: UploadFile):
    """
    Extract embedding from a pre-cropped face image.

    Args:
        file: UploadFile containing a cropped face image

    Returns:
        dict with embedding vector and metadata in JSON format
    """
    logger.info(f"Processing cropped face image: {file.filename}")

    # Read image bytes
    img_bytes = file.file.read()
    np_arr = np.frombuffer(img_bytes, np.uint8)
    img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)

    if img is None:
        logger.error("Failed to decode image")
        return {"error": "Invalid image format"}

    logger.info(f"Cropped face image size: {img.shape[1]}x{img.shape[0]}")

    try:
        # Get embedding from the cropped face
        embedding = get_embedding_from_cropped_face(img)

        # Convert embedding to list for JSON serialization
        embedding_list = embedding.tolist()

        # Return embedding with metadata
        result = {
            "embedding": embedding_list,
            "embedding_shape": list(embedding.shape),
            "embedding_dim": len(embedding_list),
            "model": "antelopev2_glint360k",
            "input_size": "112x112"
        }

        logger.info(f"Successfully extracted embedding with dimension {len(embedding_list)}")
        return result

    except Exception as e:
        logger.exception(f"Error extracting embedding: {str(e)}")
        return {"error": f"Failed to extract embedding: {str(e)}"}
