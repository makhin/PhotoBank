# CLAUDE Guidance — packages/tv

## Контекст
- React Native TV OS приложение для PhotoBank, работает в монорепе pnpm.
- Базовые технологии: React 19, React Native TVOS 0.81, TypeScript, React Query, Zustand, React Navigation.
- Использует @photobank/shared для доступа к общим типам (не хукам, т.к. API-клиент на axios отличается от fetch в shared).

## Рабочий процесс
- Устанавливай зависимости через workspace: `pnpm -w i` из корня frontend/
- Генерируй API-типы:
  - Локально из tv/: `pnpm run generate:api` (использует ../../orval.config.ts)
  - Из корня frontend/: `pnpm run orval:tv` или `pnpm run orval` (для всех проектов)
- Централизованная конфигурация orval в frontend/orval.config.ts (проект "tv")
- Запускай Metro: `pnpm start`, затем устройство через `pnpm run android`/`pnpm run ios`
- Придерживайся Conventional Commits, линти через `pnpm lint:fix`, форматируй `pnpm format`

## Архитектура
- API-клиент: axios с AsyncStorage для токенов (src/api/client.ts), кастомный mutator для orval
- Состояние: Zustand для глобального (auth, credentials), React Query для серверного
- Навигация: React Navigation v7 (native-stack), экраны в src/screens/
- TV-специфика: используй hasTVPreferredFocus, nextFocus*, onFocus/onBlur для управления фокусом
- Типы tv-расширений (TVFocusGuideView, useTVEventHandler) описаны в src/types/tv.d.ts

## Качество
- Типизация строгая (strict: true), покрывай критичные компоненты тестами (jest)
- Перед коммитом: `pnpm lint`, `pnpm type-check`, `pnpm test`
- При обновлении openapi.yaml перегенерируй типы и проверь совместимость

## Интеграция с монорепой
- tv находится в frontend/packages/, но имеет свою нативную сборку и зависимости
- Не обновляй пакеты в tv без явного запроса (npm scripts и версии фиксированы)
- При добавлении новых API-эндпоинтов синхронизируй с root openapi.yaml и запускай generate:api
- Конфигурация orval централизована в frontend/orval.config.ts (два проекта: frontend для web, tv для React Native)
- API типы генерируются из общего openapi.yaml, но с разными клиентами (fetch для web, axios для tv)

## Особенности pnpm монорепы
- Gradle ищет @react-native/codegen в локальном node_modules, но pnpm поднимает его на уровень выше
- После установки зависимостей автоматически создаётся symlink через postinstall хук
- При проблемах со сборкой запусти вручную: `pnpm run setup-symlinks`
- Пути в android/app/build.gradle настроены на ../../node_modules (относительно packages/tv/)
