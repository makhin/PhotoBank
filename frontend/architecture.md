PhotoBank — Frontend Architecture

> Target package: packages/frontend (React 18, Vite, Vitest 3.x, RTL, Redux Toolkit, shadcn/ui, AG Grid, pnpm monorepo). Aliases: @ → src, @photobank/shared → ../shared/src.



1. Goals & Principles

Modularity & Reuse: UI is composed from small, testable units. Prefer composition over inheritance.

Separation of Concerns: UI (presentational) ≠ State/Business logic (containers, hooks, slices).

Predictability: Redux Toolkit for app state, co-located feature logic, typed contracts.

Testability: RTL for behavior tests, Vitest for unit/integration, stable test setup.

Sustainability: Clear layering & naming, consistent folder conventions, code style enforced by ESLint/Prettier.


2. High-Level Structure (Feature-Sliced Inspired)

src/
  app/                # App shell: providers, router, layout, global styles
  pages/              # Page-level compositions for routes
  widgets/            # Complex UI blocks composed from features/entities
  features/           # Business capabilities (auth, filters, sorting, upload)
  entities/           # Domain entities (photo, user, tag)
  shared/
    ui/               # Reusable UI atoms/molecules (Button, Input, Badge, Table, EmptyState, Pagination)
    lib/              # Utilities, hooks, formatters, API clients
    config/           # App-wide config (env, constants)
    types/            # Cross-cutting types if needed

Why:

entities exposes domain types, selectors, narrow UI (e.g., TagChip).

features encapsulates user scenarios (e.g., PhotoFilters with form + slice + hooks).

widgets combine features/entities into larger blocks (e.g., PhotoTable).

pages compose widgets into screens and wire routing.


3. Routing & App Shell

Router lives in src/app/router.tsx (lazy routes where appropriate).

Providers in src/app/providers.tsx (Redux Provider, Query client if introduced, Theme, i18n if added later).

src/app/App.tsx wires providers + router.


4. State Management

Redux Toolkit (RTK) for app state. Each feature owns its slice in features/<name>/model/*.

Selectors: colocated in model/selectors.ts; memoize with Reselect where derived data.

Async: RTK thunks or RTK Query (optional) in model/api.ts or shared/lib/api.

Initial State: never undefined for arrays/maps; normalize optional arrays: x ?? [].


Slice Template

features/photos/model/photosSlice.ts
features/photos/model/selectors.ts
features/photos/model/hooks.ts

5. UI Layer

shadcn/ui for primitives; wrap in shared/ui/* to keep a stable API tailored to the app.

Presentational components are stateless, receive all data via props.

Do not import Redux directly in shared/ui/*. Containers/hooks supply data.


Example

shared/ui/EmptyState/index.tsx
shared/ui/Pagination/index.tsx
shared/ui/Badge/index.tsx

6. AG Grid Integration (Widget)

Create a thin wrapper to isolate grid specifics:

widgets/PhotoTable/index.tsx
widgets/PhotoTable/types.ts

Props (example):

export type PhotoTableProps = {
  rows: Photo[];
  columns: AgGridColDef[];
  onRowClick?: (row: Photo) => void;
  onSortChange?: (state: SortState) => void;
  rowSelection?: 'single' | 'multiple' | 'none';
  height?: number | string;
};

Rules:

No business logic inside column definitions.

Handle resize/layout via container styles and test-friendly shims (ResizeObserver mock in test setup).


7. Features & Decomposition Targets

Start with PhotoListPage split:

features/photos/filters → form controls & state

features/photos/sort → sort state/controls

features/photos/columns → visible columns logic & selector

features/photos/pagination → page/size state & controls (shared/ui/Pagination)

widgets/PhotoFiltersBar → composes filters + sort + column selector

widgets/PhotoTable → AG Grid wrapper for rows/columns

pages/PhotoListPage → orchestrates widgets, side effects via hooks


8. Data & API

Shared clients in shared/lib/api/* or feature-scoped in features/<name>/model/api.ts.

Map DTO → ViewModel as early as possible; normalize optional fields (e.g., tags ?? []).

Types from @photobank/shared are source of truth; do not duplicate.


9. Testing Strategy

Test Runner: Vitest 3.x; environment: 'jsdom'.

Setup: test-setup.ts registered in vitest.config.ts → test.setupFiles with shims: ResizeObserver, matchMedia, IntersectionObserver (and canvas if needed). Import @testing-library/jest-dom/vitest once here.


What to Test

shared/ui: behavior (roles/labels/keyboard). Limited snapshots.

features: reducers, selectors, hooks; thunks with mocked API.

widgets: integration (filters → grid). Use findBy*/waitFor, userEvent.


Commands

pnpm -C packages/frontend test
pnpm -C packages/frontend test:ci   # vitest --run --reporter=dot --coverage

10. Styling & Theming

Tailwind + shadcn/ui; variants via props; avoid inline magic numbers.

Global styles in src/app/styles.


11. Performance

Memoize selectors and heavy computations.

Use React.memo for pure presentational components behind unstable props.

Prefer controlled renders: lift state where needed, avoid prop drilling with small context only where justified.

Code-split routes and heavy widgets.


12. Accessibility

Use semantic roles and labels; prioritize RTL queries by role/name.

Interactive components must be keyboard-accessible.


13. Error Handling

Boundary components at app/ or page-level.

Report errors via centralized handler (future: logging service hook in shared/lib).


14. Env & Config

Vite envDir already points up the tree; only VITE_-prefixed vars are exposed to client.

Put app-wide constants in shared/config.


15. CI/CD

GitHub Actions workflow frontend-tests.yml runs lint, build, tests with coverage artifact.

Optional: Preview deploy (e.g., GitHub Pages/Static hosting) for PRs.


16. Naming & Conventions

Files: PascalCase for components, camelCase for utils/hooks, kebab-case for folders allowed per team preference (consistent within layer).

Exports: index-first for components; named exports for utilities.

Types: SomethingDto for API DTO, Something for domain; Props suffix for component props.


17. Example Folder Layout (after refactor)

src/
  app/
    App.tsx
    providers.tsx
    router.tsx
  pages/
    PhotoListPage/
      index.tsx
  widgets/
    PhotoTable/
      index.tsx
      types.ts
    PhotoFiltersBar/
      index.tsx
  features/
    photos/
      model/
        photosSlice.ts
        selectors.ts
        hooks.ts
      ui/
        PhotoSortBar.tsx
        ColumnSelector.tsx
        PhotoFilters.tsx
        PaginationControls.tsx
  entities/
    photo/
      model/types.ts
      ui/PhotoBadge.tsx
    tag/
      model/types.ts
      ui/TagChip.tsx
  shared/
    ui/
      Button/index.tsx
      Input/index.tsx
      Select/index.tsx
      Badge/index.tsx
      Pagination/index.tsx
      EmptyState/index.tsx
    lib/
      api/http.ts
      hooks/useDebounce.ts
      format/formatDate.ts
    config/
      constants.ts
    types/
      index.ts

18. Migration Notes

Keep page behavior identical; do refactors behind the same props/contracts.

If imports move, provide MIGRATION.md with old→new paths and codemods if any.

Avoid breaking changes to public APIs (shared/ui). If necessary, deprecate first.


19. PR Plan (suggested)

1. Structure & Infrastructure: introduce layers, add test-setup.ts, connect in vitest.config.ts, add base shared/ui primitives, ensure build/tests green.


2. PhotoListPage Decomposition: extract widgets/features, implement PhotoTable wrapper, move logic to hooks/selectors, update routes.


3. Coverage & Cleanup: write tests for new parts, raise coverage to targets, consolidate duplicates, add docs (README in each new folder), wire CI coverage.



20. Checklist

[ ] Layers created (entities, features, widgets, pages, shared/ui, shared/lib).

[ ] test-setup.ts added and connected; flaky tests stabilized.

[ ] PhotoListPage split; AG Grid wrapper in place.

[ ] Redux slices/selectors/hooks organized per feature.

[ ] UI primitives extracted to shared/ui.

[ ] CI workflow runs lint/build/tests with coverage.

[ ] README/MIGRATION updated.



---

Appendix A — Vitest Config (example)

// vitest.config.ts
import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@photobank/shared': path.resolve(__dirname, '../shared/src'),
      '@photobank/shared/': path.resolve(__dirname, '../shared/src/'),
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: './test-setup.ts',
    globals: true,
    css: true,
  },
});

Appendix B — Test Setup (example)

// test-setup.ts
import '@testing-library/jest-dom/vitest';

class ResizeObserver { observe() {} unobserve() {} disconnect() {} }
(globalThis as any).ResizeObserver = ResizeObserver;

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    media: query,
    matches: false,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});

Object.defineProperty(window, 'IntersectionObserver', {
  writable: true,
  value: class { observe() {} unobserve() {} disconnect() {} takeRecords() { return []; } },
});

Appendix C — Coding Guidelines (quick)

One responsibility per file; components <200 lines prefer split.

No business logic in shared/ui; data via props.

Prefer findBy*/waitFor + userEvent in tests; avoid timing hacks.

Avoid as any; normalize optional fields instead (e.g., tags ?? []).


