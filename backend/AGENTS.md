# Backend Scope (backend)

## Stack
- .NET 9, Minimal APIs / ASP.NET Core
- EF Core (PostgreSQL)
- Serilog structured logging
- ProblemDetails (RFC 7807)
- HealthChecks (PostgreSQL)
- Testcontainers (PostgreSQL) for integration tests

## Hard Rules
- **Work ONLY in `/backend`**.
- Integration tests MUST use PostgreSQL Testcontainers, apply EF migrations, and clean data (Respawn or similar).
- Respect access-control rules: admin sees all; regular user sees storages/people/date ranges per granted permissions.
- Prefer **keyset pagination** over offset for large datasets.

## Tasks Allowed
- Entities/DbContext/migrations (no destructive changes without migration plan).
- New endpoints (Admin Users CRUD; Access management).
- ProblemDetails mapping; Serilog enrichment.
- Health checks for DB.
- ETag for static/immutable resources.

## Performance & DB
- Favor async queries, projections (Select), `AsNoTracking` for read-only.
- Consider `ExecuteUpdate/ExecuteDelete` for batch ops (EF Core 7+).
- Remove redundant explicit transactions for single `SaveChanges()`.

## Commands
- `dotnet restore`
- `dotnet build`
- `dotnet test` (should spin up Testcontainers)
- `dotnet ef migrations add <name>` (when needed)

## Deliverables
- Passing tests (unit + integration).
- Clear migration notes.
- Sample appsettings.Development.json changes documented (no secrets committed).