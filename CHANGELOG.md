# Changelog

All notable changes to this project are documented here. Format based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Complete MCP lab: TokenService, McpServer (6 read-only tools), ChatApi (Anthropic/Mock
  `IChatClient`), BackOffice.Api, Aspire AppHost, Angular chat and BackOffice UIs, EF Core
  LocalDB schema with synthetic seed data and mock documents.
- English documentation: README, architecture (C4 + Mermaid), security model with STRIDE threat
  model, runbook.
- `specs/`: system requirements, MCP tool contract, HTTP API contracts, data model, and 15 ADRs.
- AI agent context: `AGENTS.md`, `CLAUDE.md`, `llms.txt`, `.github/copilot-instructions.md`.
- Test projects: `Procesos.UnitTests` (path guard, JWT factory, Mock LLM, repo paths) and
  `Procesos.IntegrationTests` (TokenService and McpServer authN/Z via `WebApplicationFactory`).
- GitHub Actions: CI (build, format, analyzers as errors, tests + coverage, Angular build/test/
  Prettier), CodeQL, Security (Gitleaks, dependency review, NuGet/npm audit), DAST (auth smoke
  tests + OWASP ZAP), OpenSSF Scorecard; Dependabot.
- Repository hygiene: `Directory.Build.props`, `global.json`, `.gitattributes`, CONTRIBUTING,
  SECURITY policy, CODEOWNERS, PR and issue templates.

### Fixed

- Culture-invariant formatting of the JWT `iat` claim and Mock LLM output (CA1305).
- BackOffice Angular unit test missing the router provider.
