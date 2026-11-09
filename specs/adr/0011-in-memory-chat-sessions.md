# ADR-0011: Keep chat history in process memory keyed by sessionId

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: llm, state

## Decision

`ChatSessionStore` is a singleton `ConcurrentDictionary<string, List<ChatMessage>>`. The client
sends `sessionId` (null on the first message → the server generates a GUID).

## Consequences

- Negative: history lost on restart, not shared across instances, unbounded growth, and the inner
  `List<T>` is not thread-safe for concurrent requests on the same session.
- Follow-up for production: distributed cache (e.g. Redis) with TTL and size limits, bound to an
  authenticated user.
