# System overview and requirements

## Purpose

Educational reference implementation of a complete **Model Context Protocol** pattern in
.NET 10 / C#: natural-language questions about people and their legal cases are answered by an
LLM that calls tools on a secured MCP server. A BackOffice manages the data.

## Actors

| Actor | Interface | Authentication |
|---|---|---|
| Internal user | `procesos-chat-web` | none (ADR-0013) |
| Operator | `procesos-backoffice-web` | none (ADR-0013) |
| ChatApi (service) | MCP client → McpServer | OAuth2 client credentials (ADR-0004) |
| LLM provider | Anthropic Messages API or in-process Mock | API key in User Secrets |

## In scope

- Chat in Spanish answering questions about cases, people, documents and timeline events.
- Transparency: each answer returns the list of MCP tools invoked, with arguments and results.
- CRUD for Personas, Procesos, Documentos (upload/download/delete files) and Eventos.
- Service-to-service security between ChatApi and McpServer.
- One-click local startup of backend services (Aspire).

## Out of scope

End-user authentication, HTTPS, multi-tenant deployment, cloud hosting, write actions from chat,
persistent chat history.

## Functional requirements

| ID | Requirement | Implemented in |
|---|---|---|
| FR-01 | The chat MUST answer the status of a case given its number `EXP-YYYY-NNNNN` using tool `obtener_proceso`. | ChatApi, McpServer |
| FR-02 | The chat MUST list the cases of a person given (part of) their name, via `buscar_personas` → `listar_procesos_de_persona`. | ChatApi, McpServer |
| FR-03 | The chat SHOULD quote document content via `listar_documentos_de_proceso` → `obtener_contenido_documento` (Anthropic mode). | ChatApi, McpServer |
| FR-04 | The chat MUST NOT invent case numbers, states, dates or document content; it MUST say when data is not found. | `ChatEndpoints.SystemInstructions` |
| FR-05 | Each chat response MUST include `toolCalls[]` (name, arguments, result). | `ChatEndpoints.ExtraerToolCalls` |
| FR-06 | Conversation context MUST be preserved per `sessionId` while the ChatApi process lives. | `ChatSessionStore` |
| FR-07 | The BackOffice MUST provide list/detail/create/update/delete for Personas and Procesos, with filters. | BackOffice.Api, backoffice-web |
| FR-08 | The BackOffice MUST allow uploading, downloading and deleting documents of a case. | `DocumentosEndpoints` |
| FR-09 | The BackOffice MUST allow listing, adding and deleting timeline events of a case. | `EventosEndpoints` |
| FR-10 | `DocumentoIdentidad` and `NumeroExpediente` MUST be unique; violations return 409. | `ProcesosDbContext`, endpoints |
| FR-11 | In Development the database MUST be created/migrated and seeded idempotently on BackOffice.Api startup. | `DbSeeder` |
| FR-12 | New MCP tools MUST become available to the chat without changes to ChatApi. | ADR-0002 |

## Non-functional requirements

| ID | Category | Requirement |
|---|---|---|
| NFR-01 | Security | McpServer MUST reject requests without a valid JWT (401) or without `scope=mcp.tools.invoke` (403). |
| NFR-02 | Security | Access tokens MUST expire in ≤ 5 minutes. |
| NFR-03 | Security | Secrets MUST NOT be committed; CI MUST scan for them. |
| NFR-04 | Security | File access MUST stay within `DocumentStorage:RootPath`. |
| NFR-05 | Security | `/connect/token` MUST be rate limited (30/min). |
| NFR-06 | Security | Browser-facing APIs MUST use an explicit CORS allow-list; McpServer MUST NOT enable CORS. |
| NFR-07 | Usability | The lab MUST run end to end without an LLM API key (Mock provider). |
| NFR-08 | Operability | All .NET services MUST emit OpenTelemetry traces/metrics/logs and expose health endpoints in Development. |
| NFR-09 | Maintainability | Every push/PR MUST pass build (0 warnings), tests, formatting, CodeQL and dependency/secret scans (ADR-0015). |
| NFR-10 | Portability | Each .NET service MUST be runnable stand-alone with `dotnet run` (no AppHost dependency). |

## Constraints

- .NET SDK 10, Angular 21, Node 20+, SQL Server LocalDB (Windows).
- Fixed ports: TokenService 5061, McpServer 5071, BackOffice.Api 5080, ChatApi 5090,
  chat-web 4200, backoffice-web 4300.
