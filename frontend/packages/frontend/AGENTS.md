# Frontend Scope (packages/frontend)

## Stack
- React 19, TypeScript latest
- TailwindCSS + shadcn/ui
- React Router
- State: Redux Toolkit
- Codegen API: orval (OpenAPI)

## Hard Rules
- **Work ONLY inside `packages/frontend`** (and read-only from `packages/shared`).
- Keep **mobile-first**, fix layout shifts (CLS) and accessibility basics (labels, roles).
- Avoid breaking public APIs of shared package.

## Tasks Allowed
- Pages and components (PhotoListPage, Admin Users CRUD, Filters).
- State slices, thunks/RTK Query.
- Orval client updates (types & hooks).
- Performance passes: memoization, virtualization (AG Grid), Suspense.
- Add/adjust tests (Vitest/RTL).

## Style
- shadcn/ui for primitives; Tailwind utilities.
- Use modern React patterns (use hooks, server components if configured, no legacy lifecycles).
- Keep components small; colocate tests `*.test.tsx` next to code.

## Commands
- `pnpm i`
- `pnpm dev` / `pnpm build`
- `pnpm test`

## Deliverables
- Component + story/test if applicable.
- Update docs/README when adding a new route or env var.