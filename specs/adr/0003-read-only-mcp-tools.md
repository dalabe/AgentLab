# ADR-0003: MCP tools are read-only; all writes go through the BackOffice API

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: mcp, security, llm

## Context

LLM tool calls are driven by model output, which can be influenced by user prompts and by data
returned from other tools (prompt injection, e.g. a malicious document). Autonomous writes are
high risk.

## Decision

All tools in `ProcesoTools` only query (`AsNoTracking()`), never mutate. Create/update/delete is
exclusive to `Procesos.BackOffice.Api`, operated by a human through the BackOffice UI.

## Consequences

- Positive: prompt injection can at worst distort an answer, never corrupt data.
- Negative: the chat cannot perform actions.
- Follow-up: write tools, if ever added, need a separate scope (e.g. `mcp.tools.write`) and explicit
  human confirmation in the UI before execution.

## References

- `src/Procesos.McpServer/Tools/ProcesoTools.cs`; `docs/SECURITY.md` threat T6
