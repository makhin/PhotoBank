import os
from sqlalchemy import create_engine, Column, Integer, LargeBinary, String, DateTime, ForeignKey, inspect, MetaData, Table, text
from sqlalchemy.orm import sessionmaker, declarative_base
from datetime import datetime
from dotenv import load_dotenv

# Load environment variables
load_dotenv(dotenv_path="/app/.env")

DB_URL = os.getenv("DB_URL")

# Connect to PostgreSQL database
engine = create_engine(DB_URL)
SessionLocal = sessionmaker(bind=engine)

# Base model without schema specification (PostgreSQL uses 'public' by default)
metadata = MetaData()
Base = declarative_base(metadata=metadata)

# Model for PersonEmbeddings
class PersonEmbedding(Base):
    __tablename__ = "person_embeddings"
    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    person_id = Column(Integer, ForeignKey('persons.id'))
    embedding_binary = Column(LargeBinary)
    embedding_json = Column(String)
    created_at = Column(DateTime, default=datetime.utcnow)

# Database initialization function
def init_db():
    inspector = inspect(engine)
    if not inspector.has_table("person_embeddings"):
        Base.metadata.create_all(bind=engine)

# Get list of persons from persons table
def get_persons():
    try:
        with SessionLocal() as db:
            print("Connected to DB")
            result = db.execute(text("SELECT id, name FROM persons")).fetchall()
            print(f"Query result: {result}")
            return [{"id": r[0], "name": r[1]} for r in result]
    except Exception as e:
        print(f"Error in get_persons: {e}")
        raise
