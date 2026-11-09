# Architecture Decision Records

| ADR | Title | Status | Tags |
|---|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions as ADRs | Accepted | process |
| [0002](0002-mcp-over-streamable-http.md) | MCP server over stateless HTTP | Accepted | mcp |
| [0003](0003-read-only-mcp-tools.md) | MCP tools are read-only | Accepted | mcp, security |
| [0004](0004-oauth2-client-credentials-jwt.md) | OAuth2 client credentials + short-lived JWT | Accepted | security |
| [0005](0005-hmac-signed-jwt-for-lab.md) | HS256 for the lab, RS256/JWKS for production | Accepted | security |
| [0006](0006-microsoft-extensions-ai-with-mock-provider.md) | `IChatClient` abstraction with Mock provider | Accepted | llm |
| [0007](0007-sql-server-localdb.md) | SQL Server LocalDB + EF Core migrations | Accepted | data |
| [0008](0008-aspire-orchestrates-dotnet-only.md) | Aspire orchestrates .NET services only | Accepted | tooling |
| [0009](0009-minimal-apis-typed-results-and-dtos.md) | Minimal APIs, typed results, DTOs | Accepted | api |
| [0010](0010-filesystem-document-storage.md) | Filesystem document storage + path guard | Accepted | data, security |
| [0011](0011-in-memory-chat-sessions.md) | In-memory chat sessions | Accepted | llm |
| [0012](0012-language-conventions.md) | Spanish domain, English technical/docs | Accepted | conventions |
| [0013](0013-no-end-user-authentication.md) | No end-user authentication | Accepted | security |
| [0014](0014-hardcoded-frontend-api-urls.md) | Hardcoded frontend API URLs | Accepted | frontend |
| [0015](0015-ci-quality-and-security-gates.md) | CI quality and security gates | Accepted | tooling, security |

New ADR: copy [`0000-adr-template.md`](0000-adr-template.md) to the next number and open a PR.
