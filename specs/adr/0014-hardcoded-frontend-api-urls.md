# ADR-0014: Frontend API base URLs are hardcoded constants (no build-time environments)

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: frontend

## Decision

Each Angular app keeps its API base URL in one `*.config.ts` file
(`procesos-chat-web/src/app/chat/chat-api.config.ts`, `procesos-backoffice-web/src/app/core/api.config.ts`),
matching the fixed ports in `launchSettings.json`.

## Consequences

- Positive: zero configuration for a local lab.
- Negative: deploying elsewhere requires a code edit. Follow-up: runtime `config.json` loaded at
  bootstrap, or Angular environments.
