# Environment Configuration Guide

## Понимание VITE_API_BASE_URL vs API_BASE_URL

### Ключевое различие:

- **`VITE_API_BASE_URL`** - используется фронтендом (React в браузере)
  - Запросы выполняются из **браузера пользователя**
  - URL должен быть **доступен из браузера**, не из Docker контейнера

- **`API_BASE_URL`** - используется ботом (Node.js backend)
  - Запросы выполняются из **Node.js процесса** (внутри Docker или локально)
  - URL может использовать **Docker network** (например, `http://api:5066`)

## Сценарии конфигурации

### 1️⃣ Локальная разработка (всё на localhost)

Когда вы запускаете всё локально через `npm run dev`:

```bash
# Frontend в браузере → API на localhost
VITE_API_BASE_URL=http://localhost:5066

# Bot локально → API на localhost
API_BASE_URL=http://localhost:5066

# MinIO на localhost (если запускаете локально)
MINIO_ENDPOINT=localhost:9000
```

### 2️⃣ Docker Compose (разработка в контейнерах)

Когда запускаете `docker-compose up`:

```bash
# Frontend в браузере → API на localhost (браузер обращается к host машине!)
VITE_API_BASE_URL=http://localhost:5066

# Bot в Docker → API через Docker network
API_BASE_URL=http://api:5066

# MinIO через Docker network
MINIO_ENDPOINT=minio:9000
```

**Важно:** Браузер работает **вне Docker**, поэтому `http://api:5066` не сработает для фронтенда!

### 3️⃣ Production

```bash
# Frontend в браузере → публичный API endpoint
VITE_API_BASE_URL=https://api.photobank.com

# Bot в Docker → может использовать внутреннюю сеть или публичный URL
API_BASE_URL=http://api:5066
# или
API_BASE_URL=https://api.photobank.com

# MinIO через внутреннюю сеть
MINIO_ENDPOINT=minio:9000
```

## Default значения в docker-compose.yml

В `docker-compose.yml` добавлены default значения:

```yaml
services:
  api:
    environment:
      - Minio__Endpoint=${MINIO_ENDPOINT:-minio:9000}  # default: minio:9000

  bot:
    environment:
      - API_BASE_URL=${API_BASE_URL:-http://api:5066}  # default: http://api:5066
```

Это означает:
- Если переменная **НЕ задана** в `.env` → используется default
- Если переменная **задана** в `.env` → используется значение из `.env`

## Примеры использования

### Пример 1: Запуск в Docker Compose

1. Создайте `.env` файл:
```bash
cp .env.example .env
```

2. Отредактируйте `.env`:
```bash
# Frontend - браузер обращается к localhost
VITE_API_BASE_URL=http://localhost:5066

# Bot - можно удалить эту строку, используется default http://api:5066
# API_BASE_URL=http://api:5066

# MinIO - можно удалить, используется default minio:9000
# MINIO_ENDPOINT=minio:9000
```

3. Запустите:
```bash
docker-compose up -d
```

### Пример 2: Фронтенд локально, остальное в Docker

1. Запустите backend в Docker:
```bash
docker-compose up -d api minio
```

2. В `.env`:
```bash
# Frontend локально → API на localhost
VITE_API_BASE_URL=http://localhost:5066
```

3. Запустите frontend локально:
```bash
cd frontend/packages/frontend
npm run dev
```

### Пример 3: Production с nginx reverse proxy

1. В `.env`:
```bash
# Браузер → публичный домен
VITE_API_BASE_URL=https://api.photobank.com

# Bot → внутренняя сеть Docker
API_BASE_URL=http://api:5066

# MinIO → внутренняя сеть Docker
MINIO_ENDPOINT=minio:9000
```

2. Nginx конфигурация:
```nginx
server {
    listen 443 ssl;
    server_name api.photobank.com;

    location / {
        proxy_pass http://localhost:5066;
        proxy_set_header Host $host;
    }
}
```

## Проверка конфигурации

### Проверка frontend

1. Откройте браузер → DevTools → Console
2. Выполните:
```javascript
console.log(import.meta.env.VITE_API_BASE_URL)
```

### Проверка bot

1. Проверьте логи бота:
```bash
docker-compose logs bot
```

2. Или зайдите в контейнер:
```bash
docker-compose exec bot sh
echo $API_BASE_URL
```

## Частые ошибки

### ❌ Ошибка: Frontend не может подключиться к API

**Причина:** Используете `http://api:5066` в `VITE_API_BASE_URL`

**Решение:**
```bash
# Неправильно для браузера:
VITE_API_BASE_URL=http://api:5066

# Правильно для браузера:
VITE_API_BASE_URL=http://localhost:5066
```

### ❌ Ошибка: Bot не может подключиться к API в Docker

**Причина:** Используете `http://localhost:5066` когда бот в Docker

**Решение:**
```bash
# Для бота в Docker:
API_BASE_URL=http://api:5066

# Не:
API_BASE_URL=http://localhost:5066
```

### ❌ Ошибка: CORS при локальной разработке

**Причина:** API не разрешает запросы с `http://localhost:5173`

**Решение:** Проверьте CORS настройки в `backend/PhotoBank.Api/Program.cs`

## Дополнительные переменные

### VITE_IMAGE_BASE_URL

Используется для отображения изображений в браузере:

```bash
# Локально или Docker:
VITE_IMAGE_BASE_URL=http://localhost

# Production:
VITE_IMAGE_BASE_URL=https://images.photobank.com
```

### BOT_TOKEN

Токен Telegram бота (получите у [@BotFather](https://t.me/BotFather)):

```bash
BOT_TOKEN=1234567890:ABCdefGHIjklMNOpqrsTUVwxyz
```

### Translator (AWS/Azure)

Опционально для перевода:

```bash
TRANSLATOR_ENDPOINT=https://api.translator.com
TRANSLATOR_REGION=eastus
TRANSLATOR_KEY=your_key
```

### Azure OpenAI

Опционально для AI функций:

```bash
VITE_AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
VITE_AZURE_OPENAI_KEY=your_key
VITE_AZURE_OPENAI_DEPLOYMENT=gpt-4
VITE_AZURE_OPENAI_API_VERSION=2024-02-15-preview
```

## Полезные команды

```bash
# Показать все переменные окружения в контейнере
docker-compose exec api env | grep -i minio
docker-compose exec bot env | grep -i api

# Перезапустить сервис после изменения .env
docker-compose up -d --force-recreate api

# Проверить логи
docker-compose logs -f --tail=100 api
docker-compose logs -f --tail=100 bot

# Проверить healthcheck MinIO
docker-compose exec minio mc ready local
```

## Troubleshooting

### Frontend не видит переменные окружения

Vite встраивает переменные на **этапе сборки**. После изменения `.env`:

1. Для локальной разработки - перезапустите dev сервер:
```bash
# Ctrl+C и затем:
npm run dev
```

2. Для Docker - пересоберите образ:
```bash
docker-compose build frontend
docker-compose up -d frontend
```

### Bot не видит переменные окружения

Перезапустите контейнер:

```bash
docker-compose restart bot
```

Или пересоздайте:

```bash
docker-compose up -d --force-recreate bot
```
