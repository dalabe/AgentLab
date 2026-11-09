# MCP server contract (`Procesos.McpServer`)

## Transport and security

| Property | Value |
|---|---|
| Endpoint | `http://localhost:5071/` (`MapMcp()` at root) |
| Transport | MCP Streamable HTTP, **stateless** session mode |
| SDK | `ModelContextProtocol.AspNetCore` 2.4.0 |
| Authentication | `Authorization: Bearer <JWT>` issued by TokenService (HS256, iss `procesos-token-service`, aud `procesos-mcp-server`) |
| Authorization | Policy `McpToolsInvoke`: claim `scope` = `mcp.tools.invoke` |
| Responses | 401 missing/invalid/expired token · 403 wrong scope |
| CORS | none (service-to-service only) |
| Side effects | none — all tools are read-only (ADR-0003) |

All tool results are JSON strings serialised with `JsonSerializerDefaults.Web` (camelCase),
except `obtener_contenido_documento` which returns raw text. Errors are raised as `McpException`
with a Spanish message and surface to the client as MCP tool errors.

## Tools

### `buscar_personas`

Search people by partial name (case-insensitive `LIKE` on first names, last names or full name)
or exact national ID. Max 20 results ordered by last name.

| Param | Type | Required | Description |
|---|---|---|---|
| `texto` | string | yes | Name fragment or exact `DocumentoIdentidad` |

Result: `PersonaResumenDto[]`

```json
[{ "personaId": 1, "nombreCompleto": "Juan Carlos Pérez Londoño", "documentoIdentidad": "1020304050", "email": "…", "telefono": "…" }]
```

### `listar_procesos_de_persona`

| Param | Type | Required |
|---|---|---|
| `personaId` | int | yes |

Result: `{ "persona": string, "procesos": ProcesoResumenDto[] }` ordered by last update desc.
`ProcesoResumenDto = { numeroExpediente, tipo, estado, entidadOJuzgado, fechaInicio, fechaUltimaActualizacionUtc }`.
Error: person not found.

### `obtener_proceso`

| Param | Type | Required | Example |
|---|---|---|---|
| `numeroExpediente` | string | yes | `EXP-2024-00031` |

Result: `ProcesoDetalleDto`

```json
{
  "numeroExpediente": "EXP-2024-00031", "tipo": "Laboral", "estado": "Activo",
  "entidadOJuzgado": "…", "descripcion": "…", "fechaInicio": "2024-02-01",
  "fechaUltimaActualizacionUtc": "…", "notas": null,
  "persona": { "personaId": 1, "nombreCompleto": "…", "documentoIdentidad": "…", "email": null, "telefono": null },
  "documentos": [{ "documentoId": 3, "nombre": "…", "tipo": "Demanda", "fechaCargaUtc": "…", "tamanoBytes": 1234 }],
  "eventos": [{ "fecha": "2024-02-01", "tipo": "Presentacion", "descripcion": "…" }]
}
```

Error: case not found.

### `listar_documentos_de_proceso`

| Param | Type | Required |
|---|---|---|
| `numeroExpediente` | string | yes |

Result: `DocumentoResumenDto[]` ordered by upload date (metadata only, no content).

### `listar_eventos_proceso`

| Param | Type | Required |
|---|---|---|
| `numeroExpediente` | string | yes |

Result: `EventoDto[]` ordered chronologically.

### `obtener_contenido_documento`

| Param | Type | Required |
|---|---|---|
| `numeroExpediente` | string | yes |
| `documentoId` | int | yes — must belong to that case |

Result: raw UTF-8 text of the file. Path resolved through `DocumentPathResolver` (traversal → error
"Ruta de documento inválida."). Errors: case not found, document not in case, physical file missing.

## Intended tool chains (encoded in ChatApi system prompt)

| User intent | Chain |
|---|---|
| Case status by number | `obtener_proceso` |
| Cases of a person by name | `buscar_personas` → `listar_procesos_de_persona` |
| Document content | `listar_documentos_de_proceso` → `obtener_contenido_documento` |
| Latest movement | `listar_eventos_proceso` (or `obtener_proceso`) |

## Adding a tool (checklist)

1. Method in `ProcesoTools` (or a new `[McpServerToolType]` class) with
   `[McpServerTool(Name = "snake_case_spanish")]` and `[Description]` on the method **and** every parameter.
2. Read-only, `AsNoTracking()`, bounded result size, `McpException` for not-found.
3. New class → `.WithTools<T>()` in `Procesos.McpServer/Program.cs`.
4. Document it in this file and, if it changes chains, in `ChatEndpoints.SystemInstructions`.
