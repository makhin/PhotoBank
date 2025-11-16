# InsightFace ArcFace API

REST API для детекции лиц и извлечения эмбеддингов с использованием InsightFace ArcFace модели (Glint360K).

## Модель

Сервис использует **antelopev2** model pack с моделью распознавания **ResNet100@Glint360K** для получения высококачественных эмбеддингов лиц.

## Основные возможности
- **Детекция лиц** на полных изображениях
- Извлечение 512-мерных эмбеддингов из обрезанных лиц
- **Опциональное извлечение атрибутов лица**: возраст, пол, поза головы
- Без привязки к базе данных - чистый сервис детекции и извлечения эмбеддингов
- Swagger UI доступен по адресу `/docs`

## Быстрый старт

```bash
docker compose up --build
```

## API Endpoints

| Method | Endpoint     | Description                                               |
|--------|--------------|-----------------------------------------------------------|
| POST   | /detect      | Обнаружить все лица на изображении                        |
| POST   | /embed       | Получить эмбеддинг из обрезанного изображения лица (опционально с атрибутами) |
| GET    | /health      | Проверка здоровья сервиса                                 |

## Endpoint: /detect

### Описание
Принимает изображение и обнаруживает все лица на нем, возвращая информацию о каждом лице.

### Входные данные
- Изображение (может содержать несколько лиц)
- Поддерживаемые форматы: JPEG, PNG

### Пример запроса

```bash
curl -X POST "http://localhost:5555/detect" \
  -H "accept: application/json" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@photo.jpg"
```

### Пример ответа

```json
{
  "faces": [
    {
      "id": "0",
      "score": 0.99,
      "bbox": [100, 150, 250, 300],
      "landmark": [[120, 180], [180, 180], [150, 220], [130, 250], [170, 250]],
      "age": 28,
      "gender": "male"
    }
  ]
}
```

### Поля ответа
- `faces` - массив обнаруженных лиц
  - `id` - уникальный идентификатор лица в изображении
  - `score` - уровень уверенности детекции (0-1)
  - `bbox` - координаты ограничивающей рамки [left, top, right, bottom]
  - `landmark` - координаты ключевых точек лица (глаза, нос, рот)
  - `age` - предполагаемый возраст
  - `gender` - пол ("male" или "female")

## Endpoint: /embed

### Описание
Принимает изображение с **уже обрезанным лицом** и возвращает вектор эмбеддинга.

### Входные данные
- Изображение с обрезанным лицом (любой размер, будет автоматически масштабировано до 112x112)
- Поддерживаемые форматы: JPEG, PNG

### Пример запроса

```bash
curl -X POST "http://localhost:5555/embed" \
  -H "accept: application/json" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@cropped_face.jpg"
```

### Пример ответа

```json
{
  "embedding": [0.123, -0.456, 0.789, ...],
  "embedding_shape": [512],
  "embedding_dim": 512,
  "model": "antelopev2_glint360k",
  "input_size": "112x112"
}
```

### Поля ответа
- `embedding` - вектор эмбеддинга (массив из 512 чисел float)
- `embedding_shape` - форма тензора эмбеддинга
- `embedding_dim` - размерность вектора (512 для ArcFace)
- `model` - используемая модель
- `input_size` - размер входного изображения для модели
- `attributes` - (опционально) атрибуты лица, если указан параметр `include_attributes=true`

### Параметры запроса

- `include_attributes` (query, boolean, default=false) - включить атрибуты лица в ответ

### Пример с атрибутами

```bash
curl -X POST "http://localhost:5555/embed?include_attributes=true" \
  -H "accept: application/json" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@cropped_face.jpg"
```

Ответ:
```json
{
  "embedding": [0.123, -0.456, ...],
  "embedding_shape": [512],
  "embedding_dim": 512,
  "model": "antelopev2_glint360k",
  "input_size": "112x112",
  "attributes": {
    "age": 28,
    "gender": "male",
    "pose": {
      "yaw": -5.2,
      "pitch": 2.1,
      "roll": 0.8
    }
  }
}
```


## Технические детали

- **Модель**: ResNet100 trained on Glint360K dataset
- **Размерность эмбеддинга**: 512
- **Входной размер**: 112x112 (автоматическое масштабирование)
- **Backend**: ONNX Runtime с поддержкой CUDA
- **Архитектура**: Stateless сервис без привязки к базе данных

## Оптимизация и производительность

### Выбор образа Docker

Сервис предоставляет два варианта Dockerfile:

#### 1. Dockerfile (CPU) - рекомендуется по умолчанию
```bash
docker build -t insightface-api .
```

**Характеристики:**
- Базовый образ: `python:3.11-slim`
- Multi-stage build для минимального размера
- Только CPU inference через `onnxruntime`
- Размер образа: ~800-900 MB (вместо ~1.2 GB в старой версии)
- Время запуска: ~10-15 секунд

**Оптимизации:**
- ✅ Разделение на build и runtime стадии
- ✅ Установка только runtime зависимостей в финальном образе
- ✅ Очистка apt кэшей
- ✅ Использование .dockerignore для ускорения сборки
- ✅ Встроенный healthcheck
- ✅ Закрепленные версии пакетов

#### 2. Dockerfile.gpu (GPU) - для максимальной производительности
```bash
docker build -f Dockerfile.gpu -t insightface-api-gpu .
```

**Характеристики:**
- Базовый образ: `nvidia/cuda:12.6.0-cudnn-runtime-ubuntu22.04`
- GPU inference через `onnxruntime-gpu`
- Требуется NVIDIA GPU с CUDA 12.x
- Ускорение до **3-5x** по сравнению с CPU

**Запуск:**
```bash
docker run --gpus all -p 5555:5555 insightface-api-gpu
```

### Сравнение производительности

| Конфигурация | Время inference (1 лицо) | Throughput (запросов/сек) | Рекомендации |
|--------------|-------------------------|---------------------------|--------------|
| CPU (slim)   | ~50-80ms                | ~12-20 req/s              | Легкая нагрузка, dev окружение |
| GPU (CUDA)   | ~15-25ms                | ~40-60 req/s              | Production, высокая нагрузка |

### Настройка workers

**CPU вариант:**
```bash
# Множественные workers для параллельной обработки
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "5555", "--workers", "4"]
```

**ВАЖНО для GPU:**
```bash
# Только 1 worker! Модели занимают GPU память
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "5555", "--workers", "1"]
```

**Причины:**
- InsightFace модели загружаются при старте каждого worker'а
- Модель antelopev2 весит ~300MB в памяти
- При GPU используется общая VRAM
- Множественные workers = множественные копии модели = нехватка памяти

### Рекомендации по развертыванию

#### Development (разработка)
```yaml
services:
  insightface-api:
    build: .  # CPU вариант
    ports:
      - "5555:5555"
```

#### Production (CPU)
```yaml
services:
  insightface-api:
    build: .
    ports:
      - "5555:5555"
    deploy:
      replicas: 3  # Горизонтальное масштабирование
      resources:
        limits:
          cpus: '2'
          memory: 2G
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5555/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

#### Production (GPU)
```yaml
services:
  insightface-api:
    build:
      context: .
      dockerfile: Dockerfile.gpu
    ports:
      - "5555:5555"
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    runtime: nvidia
    environment:
      - CUDA_VISIBLE_DEVICES=0
```

### Оптимизация сборки

Время сборки уменьшено благодаря:

1. **Многослойное кэширование** - изменение кода не пересобирает зависимости
2. **.dockerignore** - исключение лишних файлов из контекста сборки
3. **Закрепленные версии** - предсказуемые и быстрые сборки

### Мониторинг производительности

```bash
# Проверка здоровья
curl http://localhost:5555/health

# Время обработки одного запроса
time curl -X POST "http://localhost:5555/embed" \
  -F "file=@face.jpg" -o /dev/null -s

# Мониторинг памяти
docker stats insightface-api
```

### Дополнительная оптимизация

Если нужна еще большая производительность:

1. **Используйте NVIDIA TensorRT** вместо ONNX Runtime
   - Требует конвертации модели
   - Ускорение до 2x дополнительно

2. **Batch processing**
   - Модифицируйте API для приема нескольких изображений
   - Обрабатывайте батчами на GPU

3. **Model quantization**
   - INT8 квантизация для уменьшения размера
   - Незначительная потеря точности (~1-2%)
   - Ускорение на 30-40%

## Интеграция

Сервис предназначен для интеграции с вашим основным приложением:

1. Отправьте обрезанное изображение лица на `/embed`
2. Получите 512-мерный вектор эмбеддинга
3. Сохраните эмбеддинг в вашей базе данных (например, PostgreSQL с pgvector)
4. Выполняйте поиск похожих лиц и кластеризацию в вашем приложении

Все операции с данными (хранение, сравнение, кластеризация) выполняются на стороне вашего приложения.
