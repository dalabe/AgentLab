# ADR-0004: Secure service-to-service calls with OAuth2 client credentials and short-lived JWTs

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: security

## Context

There is no end-user authentication (ADR-0013), but calls from ChatApi to McpServer must follow
real-world best practices. A static shared API key never expires, cannot be scoped or revoked per
client, and carries no verifiable context.

## Decision

- `Procesos.TokenService` implements `POST /connect/token` with `grant_type=client_credentials`
  (RFC 6749 §4.4) and the standard response (§5.1).
- Clients registered in configuration (`Security:RegisteredClients`) with `AllowedScopes`;
  secrets compared in constant time; failures return a uniform `401 invalid_client`.
- Issued JWT: `iss`, `aud`, `sub`, `scope`, `jti`, `iat`, `nbf`, `exp` (5 minutes).
- McpServer validates issuer, audience, signature and lifetime, and requires policy
  `McpToolsInvoke` (`scope=mcp.tools.invoke`).
- ChatApi caches the token and renews ~30 s before expiry; the token never reaches a browser.
- `/connect/token` is rate limited (30 req/min fixed window).
- Shared constants in `Procesos.Security.McpSecurityDefaults` so issuer and validator cannot drift.

## Alternatives considered

| Option | Why rejected |
|---|---|
| Static API key header | No expiry, no scopes, no per-client revocation |
| mTLS | Heavy certificate setup for a local lab |
| Real IdP now (Entra ID, Keycloak, Duende) | External dependency; the standard protocol shape keeps this a config-only swap later |

## Consequences

- Positive: consumer code is IdP-agnostic; authorization is scope-based.
- Negative: one more service; secrets must be paired across projects (RUNBOOK §3).

## References

- `src/Procesos.TokenService/Endpoints/TokenEndpoint.cs`, `src/Procesos.Security/`
- `docs/SECURITY.md`, [`specs/http-api.md`](../http-api.md#tokenservice)
