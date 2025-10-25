# Настройка запуска Vitest тестов в IntelliJ IDEA

## Проблема

В монорепозитории с pnpm workspace есть 3 пакета с разными конфигурациями Vitest:
- `frontend/packages/frontend` - использует jsdom окружение
- `frontend/packages/shared` - использует node окружение
- `frontend/packages/telegram-bot` - использует node окружение с переменными окружения

IDEA не может автоматически определить, какую конфигурацию использовать и из какой директории запускать тесты.

## Решение

Созданы три готовые конфигурации запуска (run configurations) в директории `.idea/runConfigurations/`:

1. **Vitest - Frontend** - для тестов фронтенда
2. **Vitest - Shared** - для shared библиотеки
3. **Vitest - Telegram Bot** - для телеграм бота

## Как использовать

### Вариант 1: Использовать готовые конфигурации (рекомендуется)

1. Перезапустите IntelliJ IDEA
2. В правом верхнем углу в выпадающем меню конфигураций запуска вы увидите:
   - "Vitest - Frontend"
   - "Vitest - Shared"
   - "Vitest - Telegram Bot"
3. Выберите нужную конфигурацию и нажмите Run (▶️) или Debug (🐛)

### Вариант 2: Запуск отдельного теста

1. Откройте файл с тестом (например, `frontend/packages/frontend/test/EditProfileDialog.test.tsx`)
2. Кликните на зелёную стрелку рядом с `describe` или `test`
3. IDEA автоматически подхватит правильную конфигурацию для этого пакета

### Вариант 3: Настроить вручную

Если готовые конфигурации не подхватились:

1. Run → Edit Configurations
2. Нажмите "+" → Vitest
3. Настройте для каждого пакета:
   - **Name**: Vitest - Frontend (или другой пакет)
   - **Configuration file**: выберите соответствующий `vitest.config.ts`
   - **Working directory**: выберите директорию пакета
   - **Package manager**: pnpm
   - **Vitest package**: `frontend/node_modules/vitest`

## Почему npx vitest работает, а IDEA - нет?

Когда вы запускаете `npx vitest run --reporter=verbose` из терминала, вы уже находитесь в правильной директории пакета.

IDEA же по умолчанию пытается запустить тесты из корня монорепозитория (`/home/user/PhotoBank`), что приводит к проблемам:
- Не находится правильный `vitest.config.ts`
- Не резолвятся path aliases (`@/*`, `@photobank/shared/*`)
- Не находятся файлы `test-setup.ts`
- Неправильное окружение (jsdom vs node)

## Структура конфигураций

```
PhotoBank/
├── .idea/
│   ├── runConfigurations/
│   │   ├── Vitest___Frontend.xml
│   │   ├── Vitest___Shared.xml
│   │   └── Vitest___Telegram_Bot.xml
│   └── VITEST_SETUP.md (этот файл)
└── frontend/
    └── packages/
        ├── frontend/
        │   └── vitest.config.ts
        ├── shared/
        │   └── vitest.config.ts
        └── telegram-bot/
            └── vitest.config.ts
```

## Дополнительные настройки IDEA

### TypeScript Language Service

Убедитесь, что IDEA использует правильные TypeScript конфигурации:

1. Settings → Languages & Frameworks → TypeScript
2. Для пакета frontend должен использоваться `tsconfig.app.json`
3. Для shared и telegram-bot - `tsconfig.json`

### Node.js Interpreter

1. Settings → Languages & Frameworks → Node.js
2. Убедитесь, что выбран правильный интерпретатор Node.js
3. Рекомендуется версия Node 18+ или 20+

## Проверка работы

После настройки попробуйте:

1. Запустить конфигурацию "Vitest - Frontend"
2. Открыть файл `frontend/packages/frontend/test/EditProfileDialog.test.tsx`
3. Кликнуть на зелёную стрелку рядом с тестом

Если всё настроено правильно, тесты должны запуститься и выполниться успешно.

## Известные проблемы

### BOT_TOKEN для telegram-bot

В конфигурации "Vitest - Telegram Bot" уже настроена переменная окружения `BOT_TOKEN=test-token`. Если нужно другое значение - отредактируйте конфигурацию.

### Path Aliases

IDEA должен правильно резолвить алиасы:
- `@/*` → `src/*`
- `@photobank/shared` → `../shared/src`

Если есть проблемы с резолвингом - проверьте настройки в Settings → Editor → Code Style → TypeScript → Path mappings.
