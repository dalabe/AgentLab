## Summary

<!-- What does this PR change and why? Link issues with "Closes #123". -->

## Type of change

- [ ] feat · [ ] fix · [ ] docs · [ ] test · [ ] refactor · [ ] ci/build · [ ] security

## Architecture impact

- [ ] No architectural change
- [ ] Adds/supersedes an ADR: ADR-____
- [ ] Changes a contract — updated `specs/` (mcp-tools / http-api / data-model / system-overview)

## Checklist

- [ ] `dotnet build -p:ContinuousIntegrationBuild=true` has 0 warnings
- [ ] `dotnet test` and Angular tests pass; new behaviour is covered by tests
- [ ] `dotnet format --verify-no-changes` and Prettier are clean
- [ ] No secrets, no real personal data, no `AllowAnyOrigin`, no write MCP tools without an ADR
- [ ] Docs in English; domain identifiers in Spanish
