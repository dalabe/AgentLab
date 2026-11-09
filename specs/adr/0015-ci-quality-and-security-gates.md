# ADR-0015: GitHub Actions quality and security gates (static, dynamic, supply chain)

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: tooling, security, quality

## Context

Every change — human or AI-generated — must be verified automatically for correctness, code
quality and security before it reaches `main`.

## Decision

| Gate | Tool | Workflow | Trigger | Blocking |
|---|---|---|---|---|
| .NET build (warnings as errors in CI) + tests + coverage | `dotnet build/test`, coverlet | `ci.yml` | push / PR | yes |
| Formatting / code style | `dotnet format --verify-no-changes`, `.editorconfig` | `ci.yml` | push / PR | yes |
| .NET analyzers | `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild` (`Directory.Build.props`) | `ci.yml` | push / PR | yes |
| Angular build + unit tests | `ng build`, `ng test` (Vitest) | `ci.yml` | push / PR | yes |
| SAST | GitHub CodeQL (`csharp`, `javascript-typescript`, `actions`; `security-extended`) | `codeql.yml` | push / PR / weekly | alerts |
| Secret scanning | Gitleaks (full history) | `security.yml` | push / PR | yes |
| Vulnerable dependencies | `dotnet list package --vulnerable`, `npm audit --audit-level=high` | `security.yml` | push / PR / weekly | yes |
| Dependency review | `actions/dependency-review-action` (fail on high) | `security.yml` | PR | yes |
| DAST | OWASP ZAP baseline scan against running TokenService and McpServer | `dast.yml` | weekly / manual | report |
| Supply-chain posture | OpenSSF Scorecard | `scorecard.yml` | weekly | report |
| Dependency updates | Dependabot (nuget, npm ×2, github-actions) | `.github/dependabot.yml` | weekly | — |

Workflows declare least-privilege `permissions:` and pin every action to a full commit SHA (version in a trailing comment, updated by Dependabot).

## Consequences

- Positive: consistent automated verification; SARIF results in the GitHub Security tab.
- Negative: DB-backed flows are not exercised on Linux runners (LocalDB, ADR-0007).
- Repository settings: the `Protect main` ruleset blocks deletion and force pushes, requires a
  pull request (0 approvals, single maintainer) and an up-to-date branch, and requires every
  blocking check above plus the three CodeQL `Analyze (…)` jobs. DAST is not required because it
  is path-filtered and would never report on unrelated PRs. No bypass actors.
- Secret scanning push protection is enabled. Follow-up (manual): private vulnerability reporting.
