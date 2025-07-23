from fastapi import FastAPI, UploadFile, File, HTTPException
from .face_service import process_image, recognize_faces, register_face
from typing import List, Dict, Tuple
from .db import init_db, get_persons
import uvicorn

app = FastAPI(title="InsightFace API", version="1.0")

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
async def register(person_id: int, file: UploadFile = File(...)):
    return register_face(person_id, file)

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
