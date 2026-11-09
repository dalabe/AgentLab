# AGENTS.md — guide for AI coding agents

> Canonical instructions for any AI agent (Claude Code, Copilot, Cursor, Codex, …) working in this
> repository. `CLAUDE.md` imports this file. Keep it short, factual and up to date.

## What this repo is

**Procesos Legales Lab**: an educational, end-to-end **Model Context Protocol (MCP)** reference in
**.NET 10 / C#** + **Angular 21**. A Spanish chat answers questions about people and their legal
cases by letting an LLM call **read-only tools** on a **JWT-protected MCP server**, through an MCP
client, with OAuth2 client-credentials security between services. A BackOffice manages the data in
SQL Server LocalDB. All data is synthetic.

## Read first (in this order)

1. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — C4 views, request flow, data view.
2. [`specs/adr/README.md`](specs/adr/README.md) — why things are the way they are. **Do not
   contradict an Accepted ADR silently**; propose a superseding ADR instead.
3. [`specs/`](specs/README.md) — contracts: `mcp-tools.md`, `http-api.md`, `data-model.md`,
   `system-overview.md` (FR/NFR IDs).
4. [`docs/SECURITY.md`](docs/SECURITY.md) — security model + threat model.
5. [`docs/RUNBOOK.md`](docs/RUNBOOK.md) — setup, secrets, run, troubleshooting.

## Repository map

```
ProcesosLegalesLab.slnx        .NET solution (XML .slnx format)
Directory.Build.props          shared MSBuild: analyzers, nullable, CI warnings-as-errors
global.json                    pinned .NET SDK
src/
  Procesos.AppHost/            Aspire AppHost — starts the 4 .NET services (no config injection)
  Procesos.ServiceDefaults/    Aspire defaults: OpenTelemetry, health, resilience
  Procesos.TokenService/       :5061  POST /connect/token (OAuth2 client_credentials, rate limited)
  Procesos.McpServer/          :5071  MCP over HTTP (stateless), JWT + scope policy, Tools/ProcesoTools.cs
  Procesos.ChatApi/            :5090  POST /api/chat/messages — IChatClient + MCP tools, in-memory sessions
  Procesos.BackOffice.Api/     :5080  CRUD + document upload/download; migrates + seeds in Development
  Procesos.Data/               EF Core DbContext, entities, migrations, DbSeeder, DocumentPathResolver
  Procesos.Security/           JWT options/factory, McpSecurityDefaults, client-credentials token provider
  Procesos.Llm/                IChatClient registration (Anthropic | Mock) + MockChatClient
  Procesos.McpClientCore/      MCP client accessor + BearerTokenHandler
  procesos-chat-web/           Angular chat UI :4200
  procesos-backoffice-web/     Angular + Material admin UI :4300
tests/
  Procesos.UnitTests/          xUnit — pure logic (path resolver, JWT factory, mock LLM)
  Procesos.IntegrationTests/   xUnit + WebApplicationFactory — TokenService and McpServer auth
mock-data/documentos/          physical .txt documents referenced by the Documentos table
docs/  specs/  .github/        documentation, specifications/ADRs, CI workflows
```

## Commands

```powershell
dotnet build ProcesosLegalesLab.slnx                 # must be 0 warnings, 0 errors
dotnet test  ProcesosLegalesLab.slnx                 # unit + integration (no DB needed)
dotnet format ProcesosLegalesLab.slnx --verify-no-changes
cd src/procesos-chat-web;       npm ci; npm run build; npx ng test --watch=false
cd src/procesos-backoffice-web; npm ci; npm run build; npx ng test --watch=false
dotnet tool restore; dotnet tool run dotnet-ef migrations add <Name> --project src/Procesos.Data
```

Running the full system needs Windows + LocalDB + User Secrets (see RUNBOOK §3). Do **not** try to
start it in Linux sandboxes; rely on build + tests.

## Conventions (enforced in review)

- **Language** (ADR-0012): domain names in Spanish (`Persona`, `Proceso`, `obtener_proceso`,
  `/api/procesos`); generic technical names in English; **all docs, commits and PR text in English**.
- **APIs** (ADR-0009): minimal APIs, `MapGroup` + `Map*Endpoints` extensions, `Results<…>` typed
  results, DTO records in `Dtos/` with `ToListItemDto()/ToDetailDto()/ApplyTo()`. Never return EF entities.
- **Enums**: stored as string (`HasConversion<string>()`), serialised as string.
- **Files**: always `DocumentPathResolver.ResolveFullPath(...)`; never `Path.Combine` with user/DB input directly.
- **Secrets**: never in `appsettings*.json`; leave a `//` comment naming the key; use `dotnet user-secrets`.
- **MCP tools** (ADR-0003): read-only, `AsNoTracking()`, bounded results, `[Description]` on method
  and every parameter, `McpException` for not-found. Update `specs/mcp-tools.md`.
- **Security constants**: use `McpSecurityDefaults`; do not hardcode issuer/audience/scope strings.
- **Nullable** enabled everywhere; treat warnings as errors (CI sets `ContinuousIntegrationBuild=true`).
- Angular: standalone components, signals, services in `core/`, Prettier config per app.

## Definition of done for a change

- [ ] Builds with 0 warnings; `dotnet test` and Angular tests pass; `dotnet format` clean.
- [ ] New behaviour has tests (unit for logic, integration for HTTP/auth behaviour).
- [ ] Specs updated if a contract changed (`specs/*.md`); ADR added/superseded if a decision changed.
- [ ] No secrets, no real personal data, no new CORS `*`, no write MCP tools without an ADR.
- [ ] Docs in English; domain identifiers in Spanish.

## Common tasks

| Task | Where |
|---|---|
| Add MCP tool | `src/Procesos.McpServer/Tools/` → `specs/mcp-tools.md#adding-a-tool-checklist` |
| Add entity | `Procesos.Data/Entities` + `OnModelCreating` + migration + BackOffice DTOs/endpoints + `specs/data-model.md` |
| Change LLM provider | `src/Procesos.Llm/ServiceCollectionExtensions.cs` (register another `IChatClient`) |
| Change system prompt | `ChatEndpoints.SystemInstructions` in `src/Procesos.ChatApi/Endpoints/ChatEndpoints.cs` |
| Mock LLM behaviour | `src/Procesos.Llm/MockChatClient.cs` |
| JWT validation | `Procesos.Security/ServiceCollectionExtensions.AddProcesosResourceAuthentication` |
| JWT issuance | `Procesos.Security/JwtAccessTokenFactory.cs` via `Procesos.TokenService/Endpoints/TokenEndpoint.cs` |
| Frontend API URLs | `src/procesos-*/src/app/**/*.config.ts` (ADR-0014) |

## Boundaries — do not

- Commit secrets, `.pfx/.pem`, `appsettings.*.local.json`, `bin/`, `obj/`, `dist/`, `.angular/`, `node_modules/`.
- Enable CORS on McpServer, or `AllowAnyOrigin` anywhere.
- Add write/delete MCP tools, or bypass `DocumentPathResolver`.
- Edit existing EF migrations; add a new one instead.
- Weaken CI gates (remove CodeQL, secret scan, warnings-as-errors) without a superseding ADR.
