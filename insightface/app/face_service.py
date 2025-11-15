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

# Initialize InsightFace recognition model only (no face detection)
# Using antelopev2 model pack which includes ResNet100@Glint360K recognition model
logger.info("Initializing InsightFace ArcFace recognition model (antelopev2 with glint360k)...")

# Load the model pack to access models
app_insightface = insightface.app.FaceAnalysis(
    name='antelopev2',
    providers=['CUDAExecutionProvider', 'CPUExecutionProvider']
)
app_insightface.prepare(ctx_id=0, det_size=(640, 640))

# Extract models from the pack
recognition_model = None
genderage_model = None

for task_name, model in app_insightface.models.items():
    logger.info(f"Found model: {task_name} - {type(model).__name__}")

    # Extract recognition model
    if hasattr(model, 'get_feat') or 'rec' in str(type(model)).lower():
        recognition_model = model
        logger.info(f"Extracted recognition model: {type(model).__name__}")

    # Extract gender/age model
    if 'ga' in task_name.lower() or 'genderage' in str(type(model)).lower():
        genderage_model = model
        logger.info(f"Extracted genderage model: {type(model).__name__}")

if recognition_model is None:
    raise RuntimeError("Could not extract recognition model from antelopev2 pack")

logger.info(f"InsightFace models initialized - Recognition: ✓, GenderAge: {'✓' if genderage_model else '✗'}")


def preprocess_face_image(img):
    """
    Preprocess and resize face image to 112x112 for ArcFace model.
    Handles BGR, RGB, grayscale, and RGBA images.

    Args:
        img: numpy array of face image

    Returns:
        numpy array resized to 112x112 in RGB format
    """
    # Resize to 112x112 as required by ArcFace models
    if img.shape[:2] != (112, 112):
        img = cv2.resize(img, (112, 112), interpolation=cv2.INTER_LINEAR)

    # Ensure image is in RGB format for InsightFace
    if len(img.shape) == 2:  # Grayscale
        img = cv2.cvtColor(img, cv2.COLOR_GRAY2RGB)
    elif img.shape[2] == 4:  # RGBA
        img = cv2.cvtColor(img, cv2.COLOR_RGBA2RGB)
    # Note: cv2 reads images in BGR, but InsightFace models handle this internally

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

    # Extract embedding using the recognition model
    embedding = recognition_model.get_feat(processed_img)

    return embedding


def get_face_attributes(img):
    """
    Extract face attributes from a pre-cropped face image.
    Uses FaceAnalysis to detect the face and extract all attributes.

    Args:
        img: numpy array of face image

    Returns:
        dict with age, gender, and other attributes, or None if no face detected
    """
    # Use FaceAnalysis to detect face and extract attributes
    # Even though it's a cropped face, we still need to "detect" it
    faces = app_insightface.get(img)

    if not faces or len(faces) == 0:
        logger.warning("No face detected in the cropped image")
        return None

    # Take the first (and should be only) detected face
    face = faces[0]

    # Extract attributes
    attributes = {
        "age": int(face.age) if hasattr(face, 'age') and face.age is not None else None,
        "gender": "male" if hasattr(face, 'gender') and face.gender == 1 else "female" if hasattr(face, 'gender') else None,
    }

    # Add pose if available
    if hasattr(face, 'pose') and face.pose is not None:
        attributes["pose"] = {
            "yaw": float(face.pose[0]) if len(face.pose) > 0 else None,
            "pitch": float(face.pose[1]) if len(face.pose) > 1 else None,
            "roll": float(face.pose[2]) if len(face.pose) > 2 else None,
        }

    # Add embedding shape info
    if hasattr(face, 'embedding') and face.embedding is not None:
        attributes["embedding_available"] = True
        attributes["embedding_dim"] = len(face.embedding)

    return attributes


def register_face(face_id: int, file: UploadFile):
    """
    Register a face embedding for a specific face ID.
    Expects a pre-cropped face image, not a full photo.

    Args:
        face_id: ID of the face to register embedding for
        file: UploadFile containing a cropped face image

    Returns:
        dict with status and face_id
    """
    logger.info(f"Registering face embedding for face_id={face_id}")

    # Read image bytes
    img_bytes = file.file.read()
    np_arr = np.frombuffer(img_bytes, np.uint8)
    img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)

    if img is None:
        logger.error("Failed to decode image")
        return {"error": "Invalid image format"}

    logger.info(f"Processing cropped face image size: {img.shape[1]}x{img.shape[0]}")

    try:
        # Extract embedding from the cropped face
        embedding = get_embedding_from_cropped_face(img)

        with SessionLocal() as db:
            embedding_record = PersonEmbedding(
                face_id=face_id,
                embedding=embedding.tolist()  # pgvector accepts list/array directly
            )
            db.add(embedding_record)
            db.commit()
            logger.info(f"Face embedding registered for face_id={face_id}")
            return {
                "status": "registered",
                "face_id": face_id,
                "embedding_dim": len(embedding)
            }
    except Exception as e:
        logger.exception(f"Database error during face registration: {str(e)}")
        return {"error": f"Database error: {str(e)}"}


def embed_cropped_face(file: UploadFile, include_attributes: bool = False):
    """
    Extract embedding from a pre-cropped face image.

    Args:
        file: UploadFile containing a cropped face image
        include_attributes: whether to include face attributes (age, gender, pose)

    Returns:
        dict with embedding vector and metadata in JSON format
    """
    logger.info(f"Processing cropped face image: {file.filename}, include_attributes={include_attributes}")

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

        # Optionally add attributes
        if include_attributes:
            attributes = get_face_attributes(img)
            if attributes:
                result["attributes"] = attributes

        logger.info(f"Successfully extracted embedding with dimension {len(embedding_list)}")
        return result

    except Exception as e:
        logger.exception(f"Error extracting embedding: {str(e)}")
        return {"error": f"Failed to extract embedding: {str(e)}"}


def analyze_face(file: UploadFile):
    """
    Extract face attributes (age, gender, pose) from a pre-cropped face image.

    Args:
        file: UploadFile containing a cropped face image

    Returns:
        dict with face attributes in JSON format
    """
    logger.info(f"Analyzing face attributes for: {file.filename}")

    # Read image bytes
    img_bytes = file.file.read()
    np_arr = np.frombuffer(img_bytes, np.uint8)
    img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)

    if img is None:
        logger.error("Failed to decode image")
        return {"error": "Invalid image format"}

    logger.info(f"Face image size: {img.shape[1]}x{img.shape[0]}")

    try:
        # Get attributes from the cropped face
        attributes = get_face_attributes(img)

        if attributes is None:
            return {"error": "No face detected in the image"}

        logger.info(f"Successfully extracted attributes: {attributes}")
        return {
            "status": "success",
            "attributes": attributes
        }

    except Exception as e:
        logger.exception(f"Error analyzing face: {str(e)}")
        return {"error": f"Failed to analyze face: {str(e)}"}
