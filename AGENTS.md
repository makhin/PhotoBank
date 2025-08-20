# PhotoBank – Repo Rules (Monorepo, pnpm)

## Tech Map
- Frontend: React 19, TypeScript (latest), Tailwind + shadcn/ui.
- Backend: .NET 9, EF Core (SQL Server), Serilog, ProblemDetails (RFC 7807), HealthChecks.
- Infra/Tests: Testcontainers (MSSQL), Docker, ETag for static, keyset pagination.
- Other packages: shared (TS utils/types), telegram-bot (TypeScript, grammY).

## General Policies
- **Monorepo** managed by **pnpm**. Respect `pnpm-workspace.yaml`.
- **Scope discipline**: never modify packages outside the current working directory unless explicitly asked.
- **No secrets**: do not read/write `.env*` or secrets. Use placeholders in examples.
- **Conventional Commits**: `feat:`, `fix:`, `refactor:`, `test:`, `chore:` etc.
- **Lint/Format**: use project linters/formatters; do not add global configs unless requested.

## Commands (workspace root)
- Install: `pnpm -w i`
- Build all: `pnpm -w build`
- Test all: `pnpm -w test`

## PR Expectations
- Small, focused diffs; changelog in PR description.
- Include instructions to run/test changed part.