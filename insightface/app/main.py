from fastapi import FastAPI, UploadFile, File, Query
from .face_service import embed_cropped_face, detect_faces

app = FastAPI(
    title="InsightFace ArcFace API",
    version="3.1",
    description="Face detection and embedding extraction service using InsightFace (ArcFace Glint360K)"
)

@app.get("/health")
async def health():
    return {"status": "ok"}

@app.post("/detect")
async def detect(
    file: UploadFile = File(...),
    include_embeddings: bool = Query(True, description="Include face embeddings in response")
):
    """
    Detect all faces in a full image.

    Detects all faces in the provided image and returns information about each face
    including bounding boxes, landmarks, age, gender, and optionally embeddings.

    Args:
        file: Image file (can contain multiple faces)
        include_embeddings: If true, includes 512-dim embeddings for each face (default: true)

    Returns:
        JSON with list of detected faces and their attributes
    """
    return detect_faces(file, include_embeddings)

@app.post("/embed")
async def embed(
    file: UploadFile = File(...),
    include_attributes: bool = Query(False, description="Include face attributes (age, gender, pose)")
):
    """
    Extract face embedding from a pre-cropped face image.

    The image should contain a single cropped face.
    It will be automatically resized to 112x112 for processing.

    Args:
        file: Image file with cropped face
        include_attributes: If true, includes age, gender, and pose in response

    Returns:
        JSON with embedding vector, metadata, and optionally face attributes
    """
    return embed_cropped_face(file, include_attributes)
