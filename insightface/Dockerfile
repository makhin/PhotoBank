# ���������� �� ������ � Python + pyodbc + MS ODBC Driver 17
FROM tadeorubio/pyodbc-msodbcsql17:latest

# ��������� ����������� ��������� Python
RUN python3 -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

# ��������� pip � ������ �����������
RUN pip install --upgrade pip setuptools wheel

WORKDIR /app

# �������� ����������� � ������������� Python-����������
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# �������� ��� ����������
COPY app/ ./app/
COPY .env /app/.env

ENV PYTHONPATH=/app

# ��������� API
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "5555"]
