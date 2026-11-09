# ADR-0005: Sign lab JWTs with HMAC-SHA256; RS256 + JWKS required for production

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: security

## Context

One issuer (TokenService) and one validator (McpServer) under the same control, running locally.

## Decision

Use HS256 with a ≥32-byte random key stored in User Secrets, set identically in both projects.
Services fail fast if the key is missing.

## Alternatives considered

| Option | Why rejected (for the lab) |
|---|---|
| RS256/ES256 + `/.well-known/jwks.json` | Key-pair generation, JWKS endpoint and rotation add complexity not needed to teach the pattern |

## Consequences

- Negative: any holder of the key can mint tokens; rotation is manual.
- **Before production**: migrate to RS256/ES256 with JWKS, or delegate to a real IdP; keep keys in a
  vault with rotation.

## References

- `src/Procesos.Security/JwtAccessTokenFactory.cs`, `src/Procesos.Security/ServiceCollectionExtensions.cs`
