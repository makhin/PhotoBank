# GitHub Actions Workflows

Этот каталог содержит GitHub Actions workflows для сборки и публикации Docker образов ARM64 для Raspberry Pi 4.

## Миграция с Woodpecker CI

Конфигурация была мигрирована из `.woodpecker/*.yml` в GitHub Actions. Все три pipeline (API, Frontend, Telegram Bot) теперь работают на GitHub с поддержкой ARM64 архитектуры.

## Workflows

### 1. API (Backend) - `api.yml`
- Сборка .NET 9.0 проекта
- Запуск unit-тестов
- Сборка и публикация Docker образа `makhin/photobank-api:latest` для **ARM64 (Raspberry Pi 4)**
- **Триггеры**: **только ручной запуск** (workflow_dispatch)

### 2. Frontend - `frontend.yml`
- Сборка Node.js 22 проекта с pnpm
- Запуск тестов
- Сборка и публикация Docker образа `makhin/photobank-frontend:latest` для **ARM64 (Raspberry Pi 4)**
- **Триггеры**: **только ручной запуск** (workflow_dispatch)

### 3. Telegram Bot - `telegram-bot.yml`
- Сборка Node.js 22 проекта с pnpm
- Запуск тестов
- Сборка и публикация Docker образа `makhin/photobank-bot:latest` для **ARM64 (Raspberry Pi 4)**
- **Триггеры**: **только ручной запуск** (workflow_dispatch)

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
1. **ARM64 поддержка**: используется QEMU для cross-platform сборки образов для Raspberry Pi 4
2. **Кэширование**: GitHub Cache для NuGet и pnpm, кэширование Docker слоев через buildx
3. **Централизация**: все на GitHub, не требуется отдельный CI сервер на Pi4
4. **Ручное управление**: workflows запускаются только вручную по требованию
5. **Бесплатно**: 2000 минут/месяц для приватных репозиториев, неограниченно для публичных

### Основные изменения:
- **Ручной запуск**: все workflows теперь запускаются только через `workflow_dispatch`
- **ARM64 сборка**: добавлен QEMU и `platforms: linux/arm64` для Raspberry Pi 4
- **Кэширование**: volumes → GitHub Actions Cache + Docker Buildx cache
- **Инфраструктура**: локальный Woodpecker на Pi4 → облачный GitHub Actions

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
