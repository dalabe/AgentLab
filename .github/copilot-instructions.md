# Copilot instructions

Follow [`AGENTS.md`](../AGENTS.md) — it is the single source of instructions for AI agents in this
repository (architecture, commands, conventions, boundaries). Architecture decisions are in
[`specs/adr/`](../specs/adr/README.md) and contracts in [`specs/`](../specs/README.md).

Key rules: Spanish domain identifiers, English docs; minimal APIs with typed results and DTOs;
read-only MCP tools; file access only via `DocumentPathResolver`; never commit secrets; keep
`dotnet build -p:ContinuousIntegrationBuild=true` at 0 warnings and tests green.
