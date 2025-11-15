# InsightFace ArcFace API

REST API для распознавания лиц с использованием InsightFace ArcFace модели (Glint360K) и MS SQL backend.

## Модель

Сервис использует **antelopev2** model pack с моделью распознавания **ResNet100@Glint360K** для получения высококачественных эмбеддингов лиц.

## Основные возможности
- **Извлечение эмбеддингов из обрезанных лиц** - новый endpoint `/embed`
- Регистрация лиц и сохранение эмбеддингов (binary + JSON)
- Поиск похожих лиц
- Пакетное распознавание
- Swagger UI доступен по адресу `/docs`

## Быстрый старт

```bash
docker compose up --build
```

## API Endpoints

| Method | Endpoint             | Description                                                  |
|--------|----------------------|--------------------------------------------------------------|
| POST   | /embed               | **Получить эмбеддинг из обрезанного изображения лица**      |
| POST   | /register            | Зарегистрировать лицо для персоны                           |
| POST   | /recognize           | Распознать лица на изображении (с детекцией)                |
| POST   | /batch_recognize     | Распознать лица на нескольких изображениях                  |
| GET    | /persons             | Получить список персон                                       |
| GET    | /health              | Проверка здоровья сервиса                                    |

## Новый endpoint: /embed

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

## Конфигурация

### .env
Настройте строку подключения к MS SQL в `.env`:
```
DB_URL=mssql+pyodbc://sa:YourPassword@localhost:1433/Photobank?driver=ODBC+Driver+18+for+SQL+Server
```

## Технические детали

- **Модель**: ResNet100 trained on Glint360K dataset
- **Размерность эмбеддинга**: 512
- **Входной размер**: 112x112 (автоматическое масштабирование)
- **Backend**: ONNX Runtime с поддержкой CUDA
