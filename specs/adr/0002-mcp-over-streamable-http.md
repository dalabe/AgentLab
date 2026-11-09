# ADR-0002: Expose domain data to the LLM through an MCP server over stateless HTTP

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: mcp, integration, llm

## Context

The core learning goal is a complete Model Context Protocol pattern in .NET: an LLM must answer
natural-language questions using real data, through tools it discovers at runtime, with both
MCP server and client in C#. The server must be a network service (not stdio) so it can be
secured with standard HTTP auth and called service-to-service.

## Decision

- `Procesos.McpServer` hosts MCP with `ModelContextProtocol.AspNetCore`
  (`AddMcpServer().WithHttpTransport(SessionMode = Stateless).WithTools<ProcesoTools>()`, `MapMcp()`).
- `Procesos.McpClientCore` wraps `ModelContextProtocol.Client.McpClient` over
  `HttpClientTransport`, created lazily once per process.
- `Procesos.ChatApi` calls `ListToolsAsync()` on **every** chat request and passes the tools
  (`McpClientTool` is an `AIFunction`) to `IChatClient`, so new server tools appear without
  client changes.
- Stateless session mode: no server-side MCP session affinity.

## Alternatives considered

| Option | Why rejected |
|---|---|
| stdio transport | Cannot be secured with OAuth2/JWT nor shared across processes |
| Direct REST + hand-written function definitions | No protocol, no runtime discovery — defeats the purpose |
| Stateful HTTP sessions | Adds affinity requirements with no benefit for read-only tools |

## Consequences

- Positive: tools are discoverable; chat side is generic; McpServer scales horizontally.
- Negative: one extra `tools/list` round trip per chat request (could be cached).

## References

- `src/Procesos.McpServer/Program.cs`, `src/Procesos.McpClientCore/ProcesosMcpClientAccessor.cs`,
  `src/Procesos.ChatApi/Endpoints/ChatEndpoints.cs`
- Spec: [`specs/mcp-tools.md`](../mcp-tools.md)
