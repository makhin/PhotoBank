# Shared Scope (packages/shared)

## Purpose
- Cross-package TypeScript types, schemas, small utilities (e.g., date formatting).

## Hard Rules
- Avoid app-specific logic; keep APIs stable.
- Any breaking change must bump version and update dependents.

## Allowed
- New types for API models (generated or hand-written)
- Utility functions with tests

## Commands
- `pnpm build`
- `pnpm test`