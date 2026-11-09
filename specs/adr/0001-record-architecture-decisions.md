# ADR-0001: Record architecture decisions as ADRs in `specs/adr`

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: process, documentation

## Context

The lab is educational: its value is as much in *why* it is built a certain way as in the code.
Decisions were scattered across Spanish prose in `docs/` and code comments. Humans and AI coding
agents need a stable, greppable, English source of truth for decisions.

## Decision

Use lightweight ADRs (MADR-inspired) stored as Markdown in `specs/adr/NNNN-kebab-title.md`,
indexed in `specs/adr/README.md`, written in English, one decision per file. ADRs are immutable
once Accepted; changing a decision means a new ADR that supersedes the old one.

## Alternatives considered

| Option | Why rejected |
|---|---|
| Wiki / external tool | Not versioned with code, invisible to AI agents working in the repo |
| Only `docs/ARCHITECTURE.md` | Mixes *what* with *why*; history of decisions is lost |

## Consequences

- Every PR that changes architecture adds or supersedes an ADR (PR template checklist).
- `AGENTS.md` and `llms.txt` point agents to the ADR index.

## References

- Template: `specs/adr/0000-adr-template.md`
