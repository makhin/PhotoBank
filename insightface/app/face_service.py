from fastapi import UploadFile
import numpy as np
import insightface
import json
import logging
import cv2
import onnxruntime as ort
from hsemotion.facial_emotions import HSEmotionRecognizer

# Setup logger
logger = logging.getLogger("face_service")
logger.setLevel(logging.INFO)

# Initialize InsightFace recognition model only (no face detection)
# Using antelopev2 model pack which includes ResNet100@Glint360K recognition model
logger.info("Initializing InsightFace ArcFace recognition model (antelopev2 with glint360k)...")

# Detect available ONNX Runtime providers
available_providers = ort.get_available_providers()
logger.info(f"Available ONNX Runtime providers: {available_providers}")

# Build provider list with preference for CUDA if available
providers = []
if 'CUDAExecutionProvider' in available_providers:
    providers.append('CUDAExecutionProvider')
    logger.info("CUDA provider is available - GPU acceleration enabled")
else:
    logger.info("CUDA provider not available - using CPU only")

if 'CPUExecutionProvider' in available_providers:
    providers.append('CPUExecutionProvider')

logger.info(f"Using providers: {providers}")

# Load the model pack to access models
app_insightface = insightface.app.FaceAnalysis(
    name='antelopev2',
    providers=providers
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

# Initialize HSEmotion model for emotion recognition
logger.info("Initializing HSEmotion model for emotion recognition...")
emotion_model = HSEmotionRecognizer(model_name='enet_b0_8_best_afew')
logger.info("HSEmotion model initialized successfully - Emotions: ✓")


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
        "gender": "male" if hasattr(face, 'gender') and face.gender == 1 else "female" if hasattr(face, 'gender') and face.gender == 0 else None,
    }

    # Add pose if available
    if hasattr(face, 'pose') and face.pose is not None:
        attributes["pose"] = {
            "yaw": float(face.pose.yaw) if hasattr(face.pose, 'yaw') else None,
            "pitch": float(face.pose.pitch) if hasattr(face.pose, 'pitch') else None,
            "roll": float(face.pose.roll) if hasattr(face.pose, 'roll') else None,
        }

    # Add embedding shape info
    if hasattr(face, 'embedding') and face.embedding is not None:
        attributes["embedding_available"] = True
        attributes["embedding_dim"] = len(face.embedding)

    return attributes


def get_emotion(img):
    """
    Detect emotion from a face image.

    Args:
        img: numpy array of face image (RGB format)

    Returns:
        dict with dominant emotion and scores for all emotions
    """
    try:
        # HSEmotion expects RGB format
        if len(img.shape) == 2:  # Grayscale
            img_rgb = cv2.cvtColor(img, cv2.COLOR_GRAY2RGB)
        elif img.shape[2] == 3:
            # Check if BGR (from cv2.imread) and convert to RGB
            img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        else:
            img_rgb = img

        # Get emotion prediction
        # HSEmotion returns: emotion as string (e.g., "Happiness"), scores as array
        emotion, scores = emotion_model.predict_emotions(img_rgb, logits=False)

        # HSEmotion returns: Anger, Contempt, Disgust, Fear, Happiness, Neutral, Sadness, Surprise
        emotion_labels = ['anger', 'contempt', 'disgust', 'fear', 'happiness', 'neutral', 'sadness', 'surprise']

        # Create scores dictionary
        emotion_scores = {label: float(score) for label, score in zip(emotion_labels, scores)}

        # emotion is already a string from HSEmotion, just convert to lowercase
        dominant_emotion = emotion.lower()

        return {
            "emotion": dominant_emotion,
            "emotion_scores": emotion_scores
        }
    except Exception as e:
        logger.warning(f"Failed to detect emotion: {str(e)}")
        return None


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

            # Add emotion detection
            emotion_data = get_emotion(img)
            if emotion_data:
                result["emotion"] = emotion_data["emotion"]
                result["emotion_scores"] = emotion_data["emotion_scores"]

        logger.info(f"Successfully extracted embedding with dimension {len(embedding_list)}")
        return result

    except Exception as e:
        logger.exception(f"Error extracting embedding: {str(e)}")
        return {"error": f"Failed to extract embedding: {str(e)}"}


def detect_faces(file: UploadFile, include_embeddings: bool = False):
    """
    Detect all faces in a full image.

    Args:
        file: UploadFile containing an image with potentially multiple faces
        include_embeddings: whether to include face embeddings in response

    Returns:
        dict with list of detected faces and their metadata in JSON format
    """
    logger.info(f"Processing image for face detection: {file.filename}, include_embeddings={include_embeddings}")

    # Read image bytes
    img_bytes = file.file.read()
    np_arr = np.frombuffer(img_bytes, np.uint8)
    img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)

    if img is None:
        logger.error("Failed to decode image")
        return {"error": "Invalid image format"}

    logger.info(f"Image size: {img.shape[1]}x{img.shape[0]}")

    try:
        # Detect all faces in the image
        faces = app_insightface.get(img)

        logger.info(f"Detected {len(faces)} face(s) in the image")

        # Convert each detected face to response format
        detected_faces = []
        for idx, face in enumerate(faces):
            face_data = {
                "id": str(idx),
                "score": float(face.det_score) if hasattr(face, 'det_score') else 1.0,
                "bbox": face.bbox.tolist() if hasattr(face, 'bbox') and face.bbox is not None else None,
                "landmark": face.kps.tolist() if hasattr(face, 'kps') and face.kps is not None else None,
                "age": int(face.age) if hasattr(face, 'age') and face.age is not None else None,
                "gender": "male" if hasattr(face, 'gender') and face.gender == 1 else "female" if hasattr(face, 'gender') and face.gender == 0 else None,
            }

            # Optionally include embedding
            if include_embeddings:
                if hasattr(face, 'embedding') and face.embedding is not None:
                    face_data["embedding"] = face.embedding.tolist()
                    face_data["embedding_dim"] = len(face.embedding)
                else:
                    face_data["embedding"] = None

            # Add emotion detection for each face
            if hasattr(face, 'bbox') and face.bbox is not None:
                try:
                    # Crop face from image using bbox
                    bbox = face.bbox.astype(int)
                    x1, y1, x2, y2 = bbox[0], bbox[1], bbox[2], bbox[3]
                    # Ensure bbox is within image bounds
                    x1, y1 = max(0, x1), max(0, y1)
                    x2, y2 = min(img.shape[1], x2), min(img.shape[0], y2)

                    face_crop = img[y1:y2, x1:x2]

                    if face_crop.size > 0:
                        emotion_data = get_emotion(face_crop)
                        if emotion_data:
                            face_data["emotion"] = emotion_data["emotion"]
                            face_data["emotion_scores"] = emotion_data["emotion_scores"]
                except Exception as e:
                    logger.warning(f"Failed to extract emotion for face {idx}: {str(e)}")

            detected_faces.append(face_data)

        return {"faces": detected_faces}

    except Exception as e:
        logger.exception(f"Error detecting faces: {str(e)}")
        return {"error": f"Failed to detect faces: {str(e)}"}

