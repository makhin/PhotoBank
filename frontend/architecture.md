# PhotoBank — Frontend Architecture (Short)

> Target: `packages/frontend` (React 18, Vite, Vitest, RTL, Redux Toolkit, shadcn/ui, pnpm monorepo)

## Принципы
- Модульность и переиспользуемость.
- Разделение UI и бизнес-логики.
- Единая структура и кодстайл.
- Тестируемость и предсказуемость.

## Потоки данных
- Redux Toolkit: slices, selectors, hooks в `features/*/model/`.
- DTO из `@photobank/shared`.
- Нормализация данных: `x ?? []`, `y ?? ''` вместо `undefined`.

## UI
- shadcn/ui для примитивов, обёртки в `shared/ui`.
- Чистые компоненты без доступа к Redux.
- Пример: `Button`, `Input`, `Pagination`, `EmptyState`.

## Тесты
- Vitest + RTL, `test-setup.ts` с шимами.
- Unit: `shared/ui`, reducers, selectors.
- Интеграция: `widgets`, сценарии фич.

## Стили
- Tailwind + shadcn/ui.
- Варианты через props, без inline magic numbers.

## PR-план
1. Базовая структура + test-setup + UI-примитивы.
2. Декомпозиция ключевых страниц.
3. Покрытие тестами, чистка дублей.

## Чеклист
- [ ] Слои созданы
- [ ] Test setup подключён
- [ ] Страницы разбиты
- [ ] Тесты зелёные
- [ ] CI: lint, build, test + coverage