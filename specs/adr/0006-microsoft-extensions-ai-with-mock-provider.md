# ADR-0006: Abstract the LLM behind `IChatClient` with a Mock provider

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: llm

## Context

The lab must work end to end right after cloning, without an API key or cost, yet support a real
model (Anthropic Claude). The tool-calling loop should not be hand-written.

## Decision

- `Procesos.Llm.AddProcesosChatClient` registers a singleton `Microsoft.Extensions.AI.IChatClient`
  chosen by `Llm:Provider`: `Anthropic` (Anthropic .NET SDK, `.AsIChatClient(model, maxTokens)`) or
  `Mock` (`MockChatClient`: regex heuristics for case numbers and person names that invoke MCP tools
  directly via `AIFunction.InvokeAsync`).
- Either client is wrapped with `.AsBuilder().UseFunctionInvocation().Build()`.
- Shipped `appsettings.json` uses `Mock`; the code default is `Anthropic`.

## Consequences

- Positive: switching provider (Azure OpenAI, OpenAI, Ollama) = registering another `IChatClient`;
  deterministic Mock enables tests and demos.
- Negative: Mock only covers two question patterns.

## References

- `src/Procesos.Llm/ServiceCollectionExtensions.cs`, `src/Procesos.Llm/MockChatClient.cs`
