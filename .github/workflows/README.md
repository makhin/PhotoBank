# GitHub Actions Workflows

Этот каталог содержит GitHub Actions workflows для автоматической сборки и публикации Docker образов.

## Миграция с Woodpecker CI

Конфигурация была мигрирована из `.woodpecker/*.yml` в GitHub Actions. Все три pipeline (API, Frontend, Telegram Bot) теперь работают на GitHub.

## Workflows

### 1. API (Backend) - `api.yml`
- Сборка .NET 9.0 проекта
- Запуск unit-тестов
- Сборка и публикация Docker образа `makhin/photobank-api:latest`
- **Триггеры**: push в main (при изменении `backend/**`), PR, manual, schedule (ежедневно в 2:00 UTC)

### 2. Frontend - `frontend.yml`
- Сборка Node.js 22 проекта с pnpm
- Запуск тестов
- Сборка и публикация Docker образа `makhin/photobank-frontend:latest`
- **Триггеры**: push в main (при изменении `frontend/packages/frontend/**` или `frontend/packages/shared/**`), PR, manual, schedule

### 3. Telegram Bot - `telegram-bot.yml`
- Сборка Node.js 22 проекта с pnpm
- Запуск тестов
- Сборка и публикация Docker образа `makhin/photobank-bot:latest`
- **Триггеры**: push в main (при изменении `frontend/packages/telegram-bot/**` или `frontend/packages/shared/**`), PR, manual, schedule

## Настройка GitHub Secrets

Для работы workflows необходимо настроить следующие secrets в GitHub репозитории:

1. Перейдите в **Settings** → **Secrets and variables** → **Actions**
2. Добавьте следующие secrets:

### Обязательные secrets:
- `DOCKER_USERNAME` - логин для Docker Hub
- `DOCKER_PASSWORD` - пароль/токен для Docker Hub

### Опциональные secrets:
- `VITE_API_BASE_URL` - базовый URL API для frontend и bot (например, `https://makhin.ddns.net/api`)

## Отличия от Woodpecker CI

### Преимущества GitHub Actions:
1. **Кэширование**: используется GitHub Cache для NuGet и pnpm, что ускоряет сборку
2. **Docker Buildx**: более эффективная сборка Docker образов с поддержкой кэширования слоев
3. **Матричные сборки**: можно легко добавить тестирование на разных платформах
4. **Интеграция**: нативная интеграция с GitHub (PR checks, статусы, комментарии)
5. **Бесплатно**: 2000 минут/месяц для приватных репозиториев, неограниченно для публичных

### Основные изменения:
- `when: event: [cron, manual]` → `on: [workflow_dispatch, schedule]`
- `when: path:` → `on: push: paths:`
- Volumes для кэширования → GitHub Actions Cache
- Docker socket → Docker Buildx actions

## Ручной запуск workflow

Чтобы запустить workflow вручную:
1. Перейдите в **Actions** → выберите workflow
2. Нажмите **Run workflow** → выберите ветку → **Run workflow**

## Мониторинг

- Все запуски workflows доступны в разделе **Actions**
- При ошибках GitHub отправит уведомление на email
- Статус последнего workflow можно добавить в README как badge:

```markdown
![API](https://github.com/makhin/PhotoBank/actions/workflows/api.yml/badge.svg)
![Frontend](https://github.com/makhin/PhotoBank/actions/workflows/frontend.yml/badge.svg)
![Telegram Bot](https://github.com/makhin/PhotoBank/actions/workflows/telegram-bot.yml/badge.svg)
```

## Удаление Woodpecker конфигурации

После успешного тестирования GitHub Actions можно удалить старую конфигурацию:
```bash
rm -rf .woodpecker/
```
