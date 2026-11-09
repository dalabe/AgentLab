# Runbook — open, configure and run the lab

## 1. Prerequisites

| Tool | Version | Check |
|---|---|---|
| Visual Studio 2022 17.14+ / VS 2026 (ASP.NET + Aspire workloads) — or VS Code + C# Dev Kit | — | — |
| .NET SDK | 10.x (verified `10.0.401`) — pinned in [`global.json`](../global.json) | `dotnet --version` |
| Node.js + npm | Node 20+ (verified `v24.14.0`, npm `11.9.0`) | `node --version` |
| SQL Server Express LocalDB (Windows) | any | `sqllocaldb info` lists `MSSQLLocalDB` |
| `dotnet-ef` (only to create migrations) | local tool, `.config/dotnet-tools.json` | `dotnet tool restore` |

## 2. Open the repository

Open `ProcesosLegalesLab.slnx` in Visual Studio. `.slnx` (XML solution) is supported natively
since VS 2022 17.14; older versions must be updated.

## 3. Secrets (User Secrets)

Secrets are **never** stored in `appsettings.json`. On a fresh clone, generate them once from the
repo root (PowerShell):

```powershell
$signingKey   = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$clientSecret = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

dotnet user-secrets set "Security:Jwt:SigningKey" "$signingKey" --project src/Procesos.TokenService
dotnet user-secrets set "Security:Jwt:SigningKey" "$signingKey" --project src/Procesos.McpServer
dotnet user-secrets set "Security:RegisteredClients:Clients:0:ClientSecret" "$clientSecret" --project src/Procesos.TokenService
dotnet user-secrets set "Security:ServiceClient:ClientSecret" "$clientSecret" --project src/Procesos.ChatApi
```

**Pairing rules**: `SigningKey` must be identical in TokenService (signs) and McpServer
(validates). `ClientSecret` must be identical in TokenService (registers) and ChatApi (sends).
Values live in `%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json`, outside the repo.

| Key | Project(s) |
|---|---|
| `Security:Jwt:SigningKey` | TokenService, McpServer |
| `Security:RegisteredClients:Clients:0:ClientSecret` | TokenService |
| `Security:ServiceClient:ClientSecret` | ChatApi |
| `Llm:Anthropic:ApiKey` (optional) | ChatApi |

### LLM: Mock vs real Anthropic

`Procesos.ChatApi` runs in **Mock mode** by default (`Llm:Provider = "Mock"` in
`src/Procesos.ChatApi/appsettings.json`): no API key needed; it answers two question patterns
with a local heuristic (`Procesos.Llm/MockChatClient.cs`). To use real Claude:

```powershell
dotnet user-secrets set "Llm:Anthropic:ApiKey" "sk-ant-..." --project src/Procesos.ChatApi
```

and set `"Llm": { "Provider": "Anthropic", ... }` in `src/Procesos.ChatApi/appsettings.json`.

## 4. First run

### 4.1 Backend (.NET): one F5 via Aspire AppHost

1. Set **Procesos.AppHost** as startup project.
2. Press **F5**. The Aspire dashboard opens showing `tokenservice`, `mcpserver`,
   `backoffice-api`, `chatapi`, each with live status and logs.
3. The **first** time `backoffice-api` starts in Development it creates the
   `ProcesosLegalesLab` LocalDB database (applies `InitialCreate`) and seeds ~8 people,
   14 cases, 22 physical documents (under `mock-data/documentos/`) and timeline events.
   Seeding is **idempotent**.

> **Angular and Aspire**: orchestrating the Angular apps from the AppHost
> (`Aspire.Hosting.JavaScript`, `AddJavaScriptApp`) was evaluated; the spawned `ng serve` exited
> ~2 s after start, reproducibly. They are therefore run separately
> ([ADR-0008](../specs/adr/0008-aspire-orchestrates-dotnet-only.md)).

### 4.2 Frontend (Angular): two terminals

```powershell
cd src/procesos-chat-web;       npm ci; npm start   # http://localhost:4200
cd src/procesos-backoffice-web; npm ci; npm start   # http://localhost:4300
```

### 4.3 Without Aspire (alternative)

```powershell
dotnet run --project src/Procesos.TokenService
dotnet run --project src/Procesos.McpServer
dotnet run --project src/Procesos.BackOffice.Api
dotnet run --project src/Procesos.ChatApi
```

Ports are fixed in each `Properties/launchSettings.json` (5061, 5071, 5080, 5090) and in the
Angular `start` scripts (4200, 4300) to match the hardcoded URLs.

## 5. Try it

**BackOffice (`http://localhost:4300`)**: browse seeded people and cases, open `EXP-2024-00031`,
see documents and timeline, download a document, upload a new one, add an event, full CRUD.

**Chat (`http://localhost:4200`)** — sample questions:

- `¿Cuál es el estado del proceso EXP-2024-00031?`
- `¿Qué procesos tiene Juan Carlos Pérez Londoño?`
- `¿Qué documentos tiene el proceso EXP-2023-00450?` (needs Anthropic mode to chain
  `listar_documentos_de_proceso` + `obtener_contenido_documento`)

The collapsible "Qué hizo" panel under each answer shows which MCP tools were called, with
arguments and results.

**`.http` files**: `Procesos.TokenService.http`, `Procesos.BackOffice.Api.http`,
`Procesos.ChatApi.http` next to each `.csproj`.

## 6. Quality checks (same as CI)

```powershell
dotnet restore ProcesosLegalesLab.slnx
dotnet build ProcesosLegalesLab.slnx -c Release
dotnet test ProcesosLegalesLab.slnx -c Release
dotnet format ProcesosLegalesLab.slnx --verify-no-changes
dotnet list ProcesosLegalesLab.slnx package --vulnerable --include-transitive
cd src/procesos-chat-web;       npm ci; npm run build; npm test -- --watch=false; npm audit --audit-level=high
cd src/procesos-backoffice-web; npm ci; npm run build; npm test -- --watch=false; npm audit --audit-level=high
```

## 7. End-to-end verification checklist

- [ ] `dotnet build ProcesosLegalesLab.slnx` succeeds with 0 warnings.
- [ ] `dotnet test` passes.
- [ ] `npm run build` succeeds in both Angular apps.
- [ ] F5 on `Procesos.AppHost` shows the 4 resources "Running".
- [ ] BackOffice shows seeded data and downloads a document.
- [ ] Chat answers the sample questions with real seed data.
- [ ] Security checklist in [`docs/SECURITY.md`](SECURITY.md#security-verification-checklist-see-also-runbookmd).

## 8. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `sqllocaldb info` does not list `MSSQLLocalDB` | Install SQL Server Express LocalDB (VS "Data storage and processing" workload) |
| 401 from McpServer | `SigningKey` differs between TokenService and McpServer, or `ClientSecret` differs between TokenService and ChatApi (step 3) |
| Service fails at startup with "Falta 'Security:Jwt:SigningKey'" | User Secrets not set for that project |
| Chat shows "No pude conectarme con el servicio de chat" | ChatApi not running on 5090 or port busy (`netstat -ano \| findstr 5090`) |
| CORS error in browser | Angular apps must run exactly on 4200/4300 (`Chat:AllowedOrigins` / `BackOffice:AllowedOrigins`) |
| `dotnet tool run dotnet-ef` fails | Run `dotnet tool restore` at repo root |
