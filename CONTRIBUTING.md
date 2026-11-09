# Contributing

Thanks for helping improve Procesos Legales Lab. Humans and AI agents follow the same rules;
agents should also read [`AGENTS.md`](AGENTS.md).

## Workflow

1. Branch from `main`: `feat/<topic>`, `fix/<topic>`, `docs/<topic>`, `chore/<topic>`.
2. Make focused changes; keep PRs small.
3. Run the local quality checks (below) — they mirror CI.
4. Open a PR using the template. CI (`CI`, `CodeQL`, `Security`) must be green before merge.
5. Squash-merge with a Conventional Commit title.

## Commit messages — Conventional Commits

```
<type>(<scope>): <imperative summary>

feat(mcp): add listar_audiencias tool
fix(security): reject tokens without jti
docs(adr): add ADR-0016 for blob storage
```

Types: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `build`, `ci`, `chore`, `security`.
Scopes: `mcp`, `chat`, `backoffice`, `security`, `data`, `llm`, `web`, `apphost`, `adr`, `ci`.

## Local quality checks

```powershell
dotnet build ProcesosLegalesLab.slnx -c Release -p:ContinuousIntegrationBuild=true   # analyzers as errors
dotnet test  ProcesosLegalesLab.slnx -c Release
dotnet format ProcesosLegalesLab.slnx --verify-no-changes
cd src/procesos-chat-web;       npm ci; npx prettier --check "src/**/*.{ts,html,css,scss}"; npm run build; npx ng test --watch=false
cd src/procesos-backoffice-web; npm ci; npx prettier --check "src/**/*.{ts,html,css,scss}"; npm run build; npx ng test --watch=false
```

## Documentation rules

- Documentation, ADRs, specs, commits and PRs are in **English**; domain identifiers stay in
  Spanish ([ADR-0012](specs/adr/0012-language-conventions.md)).
- A change to an API, MCP tool or entity updates the matching file in [`specs/`](specs/README.md).
- A change to an architectural decision adds a new ADR (copy `specs/adr/0000-adr-template.md`)
  and marks the old one *Superseded*.
- Mermaid is preferred for diagrams (rendered by GitHub).

## Security

Never commit secrets or real personal data. Report vulnerabilities privately — see [`SECURITY.md`](SECURITY.md).
