# PhotoBank — Frontend Architecture

> Target package: `packages/frontend` (React 18, Vite, Vitest 3.x, RTL, Redux Toolkit, shadcn/ui, pnpm monorepo). Aliases: `@ → src`, `@photobank/shared → ../shared/src`.

## 1. Goals & Principles
- **Modularity & Reuse:** UI из мелких, переиспользуемых, тестируемых блоков.
- **Separation of Concerns:** Презентация ≠ бизнес-логика.
- **Predictability:** Redux Toolkit для состояния, типизация через DTO и domain types.
- **Testability:** RTL для поведения, Vitest для unit/integration, единый test setup.
- **Sustainability:** Чёткие границы слоёв, единые правила именования, ESLint + Prettier.

## 2. High-Level Structure