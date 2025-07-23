# Базируемся на образе с Python + pyodbc + MS ODBC Driver 17
FROM tadeorubio/pyodbc-msodbcsql17:latest

# Установим виртуальное окружение Python
RUN python3 -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

# Обновляем pip и ставим зависимости
RUN pip install --upgrade pip setuptools wheel

WORKDIR /app

# Копируем зависимости и устанавливаем Python-библиотеки
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Копируем код приложения
COPY app/ ./app/
COPY .env /app/.env

ENV PYTHONPATH=/app

# Запускаем API
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "5555"]
