from fastapi import FastAPI, UploadFile, File, HTTPException, Query
from .face_service import register_face, embed_cropped_face, analyze_face
from typing import List, Dict, Tuple, Optional
from .db import init_db, get_persons
import uvicorn

app = FastAPI(
    title="InsightFace ArcFace API",
    version="2.0",
    description="Face recognition service using ArcFace (Glint360K) model for cropped faces"
)

@app.on_event("startup")
async def startup_event():
    init_db()

@app.get("/health")
async def health():
    return {"status": "ok"}

@app.get("/persons")
async def persons():
    return get_persons()

@app.post("/register")
async def register(face_id: int, file: UploadFile = File(...)):
    """
    Register a face embedding for a specific face ID.

    Expects a pre-cropped face image (will be resized to 112x112).
    Stores the embedding for the provided face_id.
    """
    return register_face(face_id, file)

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

@app.post("/attributes")
async def attributes(file: UploadFile = File(...)):
    """
    Extract face attributes from a pre-cropped face image.

    Analyzes the face and returns:
    - Age (estimated)
    - Gender (male/female)
    - Pose (yaw, pitch, roll angles)

    The image should contain a single cropped face.

    Returns:
        JSON with face attributes
    """
    return analyze_face(file)
