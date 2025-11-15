from fastapi import FastAPI, UploadFile, File, HTTPException
from .face_service import process_image, recognize_faces, register_face, embed_cropped_face
from typing import List, Dict, Tuple
from .db import init_db, get_persons
import uvicorn

app = FastAPI(
    title="InsightFace ArcFace API",
    version="2.0",
    description="Face recognition service using ArcFace (Glint360K) model"
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

    This endpoint detects faces in the image and stores the embedding
    for the detected face with the provided face_id.
    """
    return register_face(face_id, file)

@app.post("/recognize")
async def recognize(file: UploadFile = File(...)):
    return recognize_faces(file)

@app.post("/batch_recognize")
async def batch_recognize(files: List[UploadFile] = File(...)):
    results = []
    for file in files:
        content = await file.read()
        result = recognize_faces(content)
        results.append(result)
    return results

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
