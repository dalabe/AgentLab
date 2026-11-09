# ADR-0007: Use SQL Server LocalDB with EF Core migrations owned by BackOffice.Api

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: data

## Context

The lab owner wants a real local SQL database manageable from Visual Studio like any .NET project,
and every service runnable stand-alone with `dotnet run`.

## Decision

- SQL Server LocalDB, database `ProcesosLegalesLab`, connection string `ConnectionStrings:ProcesosDb`
  in each consuming service's `appsettings.json`.
- Schema in `Procesos.Data` (EF Core 10, Fluent configuration, enums as strings, unique indexes on
  `DocumentoIdentidad` and `NumeroExpediente`, cascade deletes).
- `Procesos.BackOffice.Api` applies migrations and seeds (`DbSeeder.SeedAsync`, idempotent) in
  Development only. McpServer only reads.
- Design-time `ProcesosDbContextFactory` so `dotnet-ef` needs no startup project.

## Alternatives considered

| Option | Why rejected |
|---|---|
| Aspire `AddSqlServer` container | Requires Docker; not a "normal" VS database; services would depend on AppHost for config |
| SQLite | Diverges from the SQL Server target |

## Consequences

- Negative: LocalDB is Windows-only; Linux CI cannot run DB-backed flows.

## References

- `src/Procesos.Data/ProcesosDbContext.cs`, `src/Procesos.Data/Seed/DbSeeder.cs`, [`specs/data-model.md`](../data-model.md)
