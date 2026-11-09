# ADR-0008: Aspire AppHost orchestrates startup of the .NET services only

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: tooling, dev-experience

## Context

Developers want one F5 to start four services, with dashboard, logs and traces.

## Decision

- `Procesos.AppHost` (Aspire 13) adds the 4 .NET projects; it does **not** inject configuration
  (`WithReference`/parameters), so each service stays runnable without the AppHost.
- Angular apps start separately with `npm start`: `Aspire.Hosting.JavaScript` `AddJavaScriptApp`
  caused `ng serve` to exit ~2 s after start, reproducibly.

## Consequences

- Positive: services independently runnable; dashboard and OpenTelemetry for free.
- Negative: three terminals instead of one; revisit when the JavaScript integration is fixed
  (comment in `src/Procesos.AppHost/AppHost.cs`).
