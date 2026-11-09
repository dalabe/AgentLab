# ADR-0012: Spanish for domain vocabulary, English for technical names and documentation

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: conventions, documentation

## Context

The domain (Latin American legal cases) and end users are Spanish-speaking; the engineering
audience and AI tooling are best served by English.

## Decision

- **Spanish**: domain entities, DTOs, routes, MCP tool names and descriptions, user-facing messages,
  seed data (`Persona`, `Proceso`, `obtener_proceso`, `/api/procesos`).
- **English**: generic technical names (`Program`, `Options`, `ServiceCollectionExtensions`), all
  repository documentation (`README.md`, `AGENTS.md`, `docs/`, `specs/`), commit messages, CI config.
- Existing Spanish XML doc comments in code are kept; be consistent within a file.

## Glossary

| Spanish | English |
|---|---|
| Persona | Person (party to a case) |
| Proceso / Expediente | Legal case / case file |
| Número de expediente | Case number (`EXP-YYYY-NNNNN`) |
| Documento de identidad | National ID number |
| Entidad o juzgado | Court or agency |
| Evento | Timeline event |
| Documento | Case document |
| Estado: Activo, EnRevision, Suspendido, Cerrado, Archivado | Active, UnderReview, Suspended, Closed, Archived |
| Tipo de proceso: Civil, Penal, Laboral, Administrativo, Comercial, Familia | Civil, Criminal, Labor, Administrative, Commercial, Family |
