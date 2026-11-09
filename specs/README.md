# Specifications

Normative, implementation-backed specifications of Procesos Legales Lab. When code and spec
disagree, treat it as a bug in one of them and fix it in the same PR.

| Spec | Scope |
|---|---|
| [`system-overview.md`](system-overview.md) | Purpose, scope, functional and non-functional requirements, constraints |
| [`mcp-tools.md`](mcp-tools.md) | MCP server contract: transport, auth, the 6 tools, inputs/outputs, errors |
| [`http-api.md`](http-api.md) | HTTP contracts of TokenService, ChatApi and BackOffice.Api |
| [`data-model.md`](data-model.md) | Entities, constraints, enums, storage of physical documents, seed data |
| [`adr/`](adr/README.md) | Architecture Decision Records (the *why*) |

Related: [`docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md) (C4 views, runtime flow),
[`docs/SECURITY.md`](../docs/SECURITY.md) (security model and threat model),
[`docs/RUNBOOK.md`](../docs/RUNBOOK.md) (operations).

Keywords **MUST**, **SHOULD**, **MAY** follow RFC 2119.
