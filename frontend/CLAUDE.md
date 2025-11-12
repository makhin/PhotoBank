# CLAUDE Guidance — frontend workspace

## Контекст
- Работай только с `frontend/*` в рамках монорепы PhotoBank на pnpm.
- Пакеты: @photobank/shared (общие типы/API), @photobank/frontend (web), @photobank/telegram-bot, @photobank/tv (React Native TV).
- Базовые технологии web: React 19, TypeScript, TailwindCSS, shadcn/ui, Vitest, Redux Toolkit, React Router.

## Рабочий процесс
- Устанавливай зависимости и запускай скрипты через workspace-команды (`pnpm -w i`, `pnpm -w build`, `pnpm -w test`).
- Генерация API: `pnpm run orval` (все проекты), `pnpm run orval:frontend` (только web), `pnpm run orval:tv` (только TV).
- Централизованная конфигурация orval в frontend/orval.config.ts с проектами "frontend" (fetch) и "tv" (axios).
- Придерживайся Conventional Commits и существующих eslint/prettier правил; не добавляй глобальные конфиги без запроса.
- Перед пушем проверяй линт, типы и тесты, следи за покрытием ≥80%.

## Архитектура и качество
- Поддерживай модульность: разделяй UI, бизнес-логику и работу с сервером.
- Компоненты проектируй mobile-first, с учётом доступности (aria, role, фокус).
- Учитывай светлую/тёмную темы и разные брейкпоинты Tailwind.
- Документируй новые маршруты, переменные окружения и команды в README рядом с изменениями.
