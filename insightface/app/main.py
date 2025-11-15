from fastapi import FastAPI, UploadFile, File, HTTPException
from .face_service import register_face, embed_cropped_face
from typing import List, Dict, Tuple
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
async def embed(file: UploadFile = File(...)):
    """
    Extract face embedding from a pre-cropped face image.

    The image should contain a single cropped face.
    It will be automatically resized to 112x112 for processing.

    Returns:
        JSON with embedding vector and metadata
    """
    return embed_cropped_face(file)
