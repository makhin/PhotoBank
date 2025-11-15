from fastapi import FastAPI, UploadFile, File, Query
from .face_service import embed_cropped_face

app = FastAPI(
    title="InsightFace ArcFace API",
    version="3.0",
    description="Face embedding extraction service using ArcFace (Glint360K) model for cropped faces"
)

@app.get("/health")
async def health():
    return {"status": "ok"}

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
