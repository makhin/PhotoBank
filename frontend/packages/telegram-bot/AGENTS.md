# Telegram Bot Scope (packages/telegram-bot)

## Stack
- TypeScript + grammY
- Talks to backend API (OpenAPI client via orval)
- Features: search by tags/people/date, "on this day", auth, paging

## Hard Rules
- **Work ONLY in `packages/telegram-bot`** (and read-only from `packages/shared`).
- Do not embed API secrets; use env placeholders.
- Keep pagination UX consistent with buttons (first/prev/next/last).

## Tasks Allowed
- Login flow updates (new backend auth).
- Commands, middleware, and API client usage.
- Snapshot tests for formatter functions.

## Commands
- `pnpm i`
- `pnpm dev`
- `pnpm test`