# CLAUDE.md

All agent instructions live in [`AGENTS.md`](AGENTS.md) (shared by every AI tool). It is imported here:

@AGENTS.md

## Claude Code specifics

- Prefer reading `specs/` and `specs/adr/` before proposing structural changes; cite ADR numbers in PR descriptions.
- Before finishing a change run: `dotnet build ProcesosLegalesLab.slnx`, `dotnet test ProcesosLegalesLab.slnx`,
  `dotnet format ProcesosLegalesLab.slnx --verify-no-changes`.
- The shell may be PowerShell on Windows; use the commands as written in `AGENTS.md`.
- Never print or log values from User Secrets.
