# ADR-0013: No end-user authentication in the lab

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: security, scope

## Context

Explicit requirement: internal educational tool; focus is service-to-service security and MCP.

## Decision

ChatApi and BackOffice.Api accept anonymous requests, restricted only by CORS allow-lists for
browser origins.

## Consequences

- Negative: anyone who can reach ports 5080/5090 can read/modify data — must stay on localhost.
- Follow-up: add OIDC (JWT bearer in APIs + Authorization Code with PKCE in Angular) before any
  shared deployment; it does not change ADR-0004.
