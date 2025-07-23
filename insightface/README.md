# insightface-api

REST API for face recognition using InsightFace and MS SQL backend.

## Features
- Register faces and save embeddings (binary + JSON)
- Search for similar faces
- Batch recognition
- Swagger UI available at `/docs`

## Quick Start

```bash
docker compose up --build
```

## API Endpoints
| Method | Endpoint             | Description                   |
|--------|----------------------|-------------------------------|
| POST   | /register            | Register a face for a person |
| POST   | /recognize           | Recognize face in image      |
| POST   | /batch/recognize     | Recognize faces in batch     |
| GET    | /persons             | Get list of persons          |
| GET    | /health              | Health check                 |

## .env
Configure your MS SQL connection string in `.env`:
```
DB_URL=mssql+pyodbc://sa:YourPassword@localhost:1433/Photobank?driver=ODBC+Driver+18+for+SQL+Server
```
