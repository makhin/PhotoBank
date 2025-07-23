import os
from sqlalchemy import create_engine, Column, Integer, LargeBinary, String, DateTime, ForeignKey, inspect, MetaData, Table, text
from sqlalchemy.orm import sessionmaker, declarative_base
from datetime import datetime
from dotenv import load_dotenv

# Загружаем переменные окружения
load_dotenv(dotenv_path="/app/.env")

DB_URL = os.getenv("DB_URL")

# Подключение к БД
engine = create_engine(DB_URL)
SessionLocal = sessionmaker(bind=engine)

# Явно указываем схему dbo
metadata = MetaData(schema="dbo")
Base = declarative_base(metadata=metadata)

# Модель для PersonEmbeddings (создаём, если нужно)
class PersonEmbedding(Base):
    __tablename__ = "PersonEmbeddings"
    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    person_id = Column(Integer, ForeignKey('Persons.Id'))
    embedding_binary = Column(LargeBinary)
    embedding_json = Column(String)
    created_at = Column(DateTime, default=datetime.utcnow)

# Функция инициализации БД
def init_db():
    inspector = inspect(engine)
    if not inspector.has_table("PersonEmbeddings", schema="dbo"):
        Base.metadata.create_all(bind=engine)

# Получение списка персон из существующей таблицы dbo.Persons
def get_persons():
    try:
        with SessionLocal() as db:
            print("Connected to DB")
            result = db.execute(text("SELECT Id, Name FROM dbo.Persons")).fetchall()
            print(f"Query result: {result}")
            return [{"id": r[0], "name": r[1]} for r in result]
    except Exception as e:
        print(f"Error in get_persons: {e}")
        raise
