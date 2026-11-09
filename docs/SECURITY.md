# Security model (service-to-service)

> Looking for how to **report a vulnerability**? See the root [`SECURITY.md`](../SECURITY.md).
> This document describes the security **design** of the lab. Decisions are recorded in
> [ADR-0004](../specs/adr/0004-oauth2-client-credentials-jwt.md),
> [ADR-0005](../specs/adr/0005-hmac-signed-jwt-for-lab.md),
> [ADR-0010](../specs/adr/0010-filesystem-document-storage.md) and
> [ADR-0013](../specs/adr/0013-no-end-user-authentication.md).

Requirement: **no end-user authentication**, but **real security best practices between
applications** (service-to-service).

## Decision: OAuth2 Client Credentials Grant + short-lived JWT

Instead of a static shared token (simple but fragile: never expires, cannot be revoked per
client, carries no verifiable context), the lab implements the standard **OAuth2 Client
Credentials Grant** (RFC 6749 §4.4), with the exact protocol shape you would use against a real
identity provider:

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=chatapi-client&client_secret=<secret>&scope=mcp.tools.invoke
```

Response (RFC 6749 §5.1):

```json
{ "access_token": "<jwt>", "token_type": "Bearer", "expires_in": 300, "scope": "mcp.tools.invoke" }
```

Because the shape is standard, **replacing `Procesos.TokenService` with a real IdP (Entra ID,
Duende IdentityServer, Keycloak) requires no change to the consumer
(`Procesos.Security/ClientCredentialsTokenProvider.cs`)** — only the `TokenEndpoint` setting.

## Components

| Role | Component | Responsibilities |
|---|---|---|
| Issuer (STS) | `Procesos.TokenService` (`Endpoints/TokenEndpoint.cs`) | Validates `client_id`/`client_secret` against `Security:RegisteredClients` using **constant-time comparison** (`CryptographicOperations.FixedTimeEquals`); checks `AllowedScopes`; issues HS256 JWT (`JwtAccessTokenFactory`) with `iss`, `aud`, `sub`=client_id, `scope`, `jti`, `iat`, `nbf`, `exp` = **5 minutes** |
| Protected resource | `Procesos.McpServer` (`Program.cs`) | `AddJwtBearer` validating issuer, audience, signature, lifetime (30 s clock skew); authorization policy `McpToolsInvoke` requires claim `scope=mcp.tools.invoke` — authentication **and** authorization |
| Service client | `Procesos.ChatApi` via `Procesos.McpClientCore/BearerTokenHandler.cs` | Requests the token, caches it in process memory and renews ~30 s before expiry (`ClientCredentialsTokenProvider`) |

Token claims (contract shared through `McpSecurityDefaults`):

| Claim | Value |
|---|---|
| `iss` | `procesos-token-service` |
| `aud` | `procesos-mcp-server` |
| `sub` | registered `client_id` (e.g. `chatapi-client`) |
| `scope` | `mcp.tools.invoke` |
| `jti` | random GUID (traceability / future replay detection) |
| `iat`, `nbf`, `exp` | issued-at, not-before, expiry (+5 min) |

## Defense-in-depth measures

- **Rate limiting** on `/connect/token` (fixed window, 30 req/min, no queue → HTTP 429) against
  brute force / credential stuffing (`Procesos.TokenService/Program.cs`).
- **CORS allow-list per known origin**, never `*`: BackOffice.Api only `http://localhost:4300`,
  ChatApi only `http://localhost:4200`. **McpServer has no CORS policy at all** — it must never be
  reachable from a browser.
- **The service token never reaches the browser**: the entire client-credentials flow happens
  between .NET processes.
- **Uniform error on bad credentials**: unknown `client_id` and wrong `client_secret` both return
  `401 invalid_client`, preventing client enumeration.
- **Path-traversal protection** for physical documents: `DocumentPathResolver.ResolveFullPath`
  normalises the combined path and checks it stays under the configured root before any file
  I/O; used by the MCP tool `obtener_contenido_documento` and by BackOffice download/upload/delete.
  Uploaded file names are reduced with `Path.GetFileName` and prefixed with a GUID.
- **Secrets out of the repository**: `SigningKey`, `ClientSecret` and the Anthropic API key are
  configured with `dotnet user-secrets`; each `appsettings.json` contains a `//` comment naming
  the exact key. Services **fail fast** at startup if `SigningKey` is missing. Secret scanning
  (Gitleaks + GitHub push protection) runs in CI.
- **Least-privilege scope**: one scope today (`mcp.tools.invoke`), but `AllowedScopes` per client
  supports finer-grained scopes (e.g. `mcp.tools.read` vs `mcp.tools.write`) without changing the
  token shape.
- **Read-only MCP surface**: all MCP tools use `AsNoTracking()` queries and never write
  ([ADR-0003](../specs/adr/0003-read-only-mcp-tools.md)), limiting the blast radius of prompt
  injection.
- **Health endpoints only in Development** (`Procesos.ServiceDefaults`).

## Threat model (STRIDE, lab scope)

| # | Threat | Asset / entry point | Mitigation in place | Residual risk / recommendation |
|---|---|---|---|---|
| T1 | **Spoofing** a service to call MCP tools | McpServer `/` | JWT signature + iss/aud + scope policy | HS256 shared key: anyone with the key can mint tokens → move to RS256/JWKS or a real IdP |
| T2 | **Brute force** of `client_secret` | TokenService `/connect/token` | Rate limit 30/min, constant-time compare, uniform 401 | Per-IP/partitioned limiter and lockout for production |
| T3 | **Token theft / replay** | Bearer token in transit | 5-min lifetime, never sent to browser | Plain HTTP on localhost → **HTTPS mandatory** outside the lab; `jti` replay cache optional |
| T4 | **Tampering** with document paths | `DocumentoProceso.RutaRelativa`, upload file name | `DocumentPathResolver` root check, `Path.GetFileName`, GUID prefix | — |
| T5 | **Stored XSS via uploaded file** | BackOffice `POST /api/procesos/{id}/documentos` + `GET /api/documentos/{id}/contenido` | Download served with stored `ContentType` and a file name (attachment) | No content-type allow-list or size limit beyond Kestrel default (~30 MB); add allow-list, `X-Content-Type-Options: nosniff`, and size cap |
| T6 | **Prompt injection** via document content or user message | LLM tool loop | Tools are read-only; system prompt forbids inventing data | A malicious document could steer the answer; do not add write tools without human-in-the-loop confirmation |
| T7 | **Information disclosure** of PII (mock) | BackOffice API, chat answers | CORS allow-list; data is synthetic | **No end-user auth**: anyone on the machine/network who can reach :5080/:5090 can read data. Bind to localhost only; add auth before any shared deployment |
| T8 | **Session hijack** of chat history | ChatApi `sessionId` | Random GUID, in-memory only | sessionId is the only isolation; acceptable for a single-user lab |
| T9 | **Denial of service** / cost abuse of LLM | ChatApi | `MaxOutputTokens=1500` | No rate limit on ChatApi; add one if exposed |
| T10 | **Vulnerable dependencies** | NuGet / npm | Dependabot, `dotnet list package --vulnerable`, `npm audit`, dependency review on PRs, CodeQL | Keep Dependabot PRs merged |
| T11 | **Secrets committed** | Git history | User Secrets, `.gitignore` for `*.pfx`/`*.pem`/`appsettings.*.local.json`, Gitleaks in CI | Enable GitHub secret scanning push protection in repo settings |

## What was NOT implemented, and why

Deliberate scope of the lab:

- **No end-user authentication** in chat or BackOffice (internal lab tool). Natural entry point if
  needed: `AddAuthentication` in ChatApi/BackOffice.Api + CORS/UI changes; it does not affect the
  service-to-service model described here.
- **Symmetric (HMAC) instead of asymmetric (RS256) + JWKS**: with one issuer and one validator
  under the same control, HMAC is sufficient and simpler. **Before taking this pattern to
  production**, migrate to RS256 with `/.well-known/jwks.json`, or to a real IdP.
- **HTTPS**: the lab runs plain HTTP on `localhost` to avoid installing dev certificates. Any real
  deployment must serve all endpoints over HTTPS only (and enable HSTS).
- **No automated rotation** of `client_secret`/`SigningKey` (static, generated once in User
  Secrets). A real IdP or Key Vault with rotation would solve this.

## Security verification checklist (see also [`RUNBOOK.md`](RUNBOOK.md))

Automated in `tests/Procesos.IntegrationTests` (run by CI):

- [x] `POST /connect/token` with correct credentials returns a valid JWT.
- [x] `POST /connect/token` with wrong `client_secret` or unknown `client_id` returns 401 with the
      same error body.
- [x] Unsupported `grant_type` returns 400 `unsupported_grant_type`.
- [x] A call to McpServer without `Authorization` returns 401.
- [x] A call to McpServer with a token lacking `scope=mcp.tools.invoke` returns 403.
- [x] `DocumentPathResolver` rejects `..` traversal and absolute paths outside the root.

Manual:

- [ ] A call to McpServer with an expired JWT (wait 5+ minutes) returns 401.
- [ ] McpServer responses contain no `Access-Control-Allow-Origin` header.
