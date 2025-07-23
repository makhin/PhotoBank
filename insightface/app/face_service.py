from .db import SessionLocal, PersonEmbedding
from fastapi import UploadFile
import numpy as np
import insightface
import json

app_insightface = insightface.app.FaceAnalysis()
app_insightface.prepare(ctx_id=1, det_size=(640, 640))

def process_image(file: UploadFile):
    img_bytes = file.file.read()
    import cv2
    np_arr = np.frombuffer(img_bytes, np.uint8)
    img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
    faces = app_insightface.get(img)
    if not faces:
        return None
    return faces[0]

def register_face(person_id: int, file: UploadFile):
    face = process_image(file)
    if face is None:
        return {"error": "No face detected"}
    emb_bin = face.embedding.tobytes()
    emb_json = json.dumps(face.embedding.tolist())
    with SessionLocal() as db:
        embedding = PersonEmbedding(
            person_id=person_id,
            embedding_binary=emb_bin,
            embedding_json=emb_json
        )
        db.add(embedding)
        db.commit()
        return {"status": "registered", "person_id": person_id}

def recognize_faces(file: UploadFile):
    face = process_image(file)
    if face is None:
        return {"error": "No face detected"}
    # Placeholder for actual comparison logic
    return {"embedding": face.embedding.tolist(), "shape": face.embedding.shape}
