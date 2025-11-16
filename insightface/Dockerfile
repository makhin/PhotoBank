# Use official Python image
FROM python:3.11-slim

# Install system dependencies for psycopg2 and opencv
RUN apt-get update && apt-get install -y \
    libpq-dev \
    gcc \
    libgl1-mesa-glx \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

# Upgrade pip and install build tools
RUN pip install --upgrade pip setuptools wheel

WORKDIR /app

# Copy and install Python dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY app/ ./app/

ENV PYTHONPATH=/app

# Start API
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "5555"]
