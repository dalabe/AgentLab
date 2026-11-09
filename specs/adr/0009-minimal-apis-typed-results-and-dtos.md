# ADR-0009: Minimal APIs with typed results and explicit DTOs; never expose EF entities

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: api

## Decision

- All HTTP services use minimal APIs grouped with `MapGroup` / `Map*Endpoints` extension methods.
- Handlers return `Results<T1, T2, ...>` (`Microsoft.AspNetCore.Http.HttpResults`) so status codes
  are documented at compile time.
- Each API has a `Dtos/` folder of records plus mapping extensions (`ToListItemDto()`,
  `ToDetailDto()`, `ApplyTo()`); entities never cross the API boundary.
- Enums serialised as strings (`JsonStringEnumConverter`).

## Consequences

- Positive: no over-posting, contracts decoupled from schema, readable JSON.
- Negative: mapping boilerplate.
