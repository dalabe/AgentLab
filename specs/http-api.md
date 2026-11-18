# HTTP API contracts

All APIs: JSON (`System.Text.Json`, camelCase), minimal APIs with typed results (ADR-0009).
Enums travel as strings. Error bodies for 404/409/400 from BackOffice are plain JSON strings
(Spanish messages).

## TokenService

Base URL `http://localhost:5061`.

### `POST /connect/token`

Content-Type `application/x-www-form-urlencoded`. Rate limit: 30 req/min (fixed window) → 429.

| Field | Required | Notes |
|---|---|---|
| `grant_type` | yes | must be `client_credentials` |
| `client_id` | yes | registered in `Security:RegisteredClients:Clients` |
| `client_secret` | yes | compared in constant time |
| `scope` | no | defaults to `mcp.tools.invoke`; must be in client's `AllowedScopes` |

| Status | Body |
|---|---|
| 200 | `{ "access_token", "token_type": "Bearer", "expires_in": 300, "scope" }` |
| 400 | `{ "error": "invalid_request" \| "unsupported_grant_type" \| "invalid_scope", "error_description" }` |
| 401 | `{ "error": "invalid_client", "error_description" }` (same body for unknown client and wrong secret) |
| 429 | rate limited |

## ChatApi

Base URL `http://localhost:5090`. CORS: `Chat:AllowedOrigins` (default `http://localhost:4200`).

### `POST /api/chat/messages`

Request `ChatRequestDto`:

```json
{ "sessionId": null, "message": "¿Cuál es el estado del proceso EXP-2024-00031?" }
```

Response 200 `ChatResponseDto`:

```json
{
  "sessionId": "9f1c…",
  "answer": "El proceso EXP-2024-00031 está Activo…",
  "toolCalls": [{ "name": "obtener_proceso", "arguments": { "numeroExpediente": "EXP-2024-00031" }, "result": "{…}" }]
}
```

400 when `message` is empty or whitespace.

## BackOffice.Api

Base URL `http://localhost:5080`. CORS: `BackOffice:AllowedOrigins` (default `http://localhost:4300`).

### Request validation (400)

Create/update bodies are validated before any database access (`Dtos/UpsertValidation.cs`); the
first failing rule is returned as a Spanish plain-string message, e.g.
`"El campo 'nombres' es obligatorio."`. Length limits equal the column sizes in
[data-model.md](data-model.md).

| DTO | Required | Max length | Other rules |
|---|---|---|---|
| `PersonaUpsertDto` | `documentoIdentidad`, `nombres`, `apellidos` | 32, 120, 120; `email` 200, `telefono` 30 | `email` must be a valid address when present |
| `ProcesoUpsertDto` | `numeroExpediente`, `personaId` (> 0), `entidadOJuzgado`, `descripcion`, `fechaInicio` | 40, —, 200, 2000; `notas` 2000 | `tipo`, `estado` must be defined enum values |
| `EventoUpsertDto` | `fecha`, `descripcion` | `descripcion` 500 | `tipo` must be a defined enum value |

Malformed JSON or unknown enum names are rejected by the framework with 400 before reaching the
endpoint.

### Personas

| Method | Route | Query / body | Responses |
|---|---|---|---|
| GET | `/api/personas` | `?q=` text filter | 200 `PersonaListItemDto[]` |
| GET | `/api/personas/{id}` | — | 200 `PersonaDetailDto`, 404 |
| POST | `/api/personas` | `PersonaUpsertDto` | 201 `PersonaDetailDto`, 400 invalid body, 409 duplicate `documentoIdentidad` |
| PUT | `/api/personas/{id}` | `PersonaUpsertDto` | 200, 400, 404, 409 |
| DELETE | `/api/personas/{id}` | — | 204, 404 (cascades to cases) |

### Procesos

| Method | Route | Query / body | Responses |
|---|---|---|---|
| GET | `/api/procesos` | `?personaId=&tipo=&estado=&q=` | 200 `ProcesoListItemDto[]` |
| GET | `/api/procesos/{id}` | — | 200 `ProcesoDetailDto`, 404 |
| GET | `/api/procesos/by-expediente/{numeroExpediente}` | — | 200 `ProcesoDetailDto`, 404 |
| POST | `/api/procesos` | `ProcesoUpsertDto` | 201, 400 invalid body, 404 persona not found, 409 duplicate `numeroExpediente` |
| PUT | `/api/procesos/{id}` | `ProcesoUpsertDto` | 200, 400, 404, 409 |
| DELETE | `/api/procesos/{id}` | — | 204, 404 (cascades documents/events rows) |

### Documentos

| Method | Route | Body | Responses |
|---|---|---|---|
| GET | `/api/procesos/{procesoId}/documentos` | — | 200 `DocumentoListItemDto[]`, 404 |
| POST | `/api/procesos/{procesoId}/documentos` | `multipart/form-data`: `archivo` (file), `nombre`, `tipo` (`TipoDocumento`) | 201 (Location `/api/documentos/{id}/contenido`), 400 empty file / `nombre` > 200 chars / invalid `tipo`, 404 |
| GET | `/api/documentos/{id}/contenido` | — | 200 file stream (stored content type, download name), 404 |
| DELETE | `/api/documentos/{id}` | — | 204 (row + physical file), 404 |

Antiforgery is disabled on upload (no cookies/auth in the lab; CORS restricts origins).

### Eventos

| Method | Route | Body | Responses |
|---|---|---|---|
| GET | `/api/procesos/{procesoId}/eventos` | — | 200 `EventoListItemDto[]`, 404 |
| POST | `/api/procesos/{procesoId}/eventos` | `EventoUpsertDto { fecha, tipo, descripcion }` | 201, 400 invalid body, 404 |
| DELETE | `/api/eventos/{id}` | — | 204, 404 |

DTO definitions: `src/Procesos.BackOffice.Api/Dtos/*.cs`. TypeScript mirrors:
`src/procesos-backoffice-web/src/app/core/models.ts`, `src/procesos-chat-web/src/app/chat/chat.models.ts`.

## Common endpoints (Development only)

`GET /health` (all checks) and `GET /alive` (liveness) on every .NET service (`Procesos.ServiceDefaults`).
