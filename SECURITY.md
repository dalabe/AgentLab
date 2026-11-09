# Security Policy

## Scope

Procesos Legales Lab is an **educational** project intended to run on a developer machine
(`localhost`, plain HTTP, no end-user authentication, synthetic data). It is **not** production
software. Known, deliberate limitations are documented in
[`docs/SECURITY.md`](docs/SECURITY.md#what-was-not-implemented-and-why) and are not considered
vulnerabilities.

We do want to hear about:

- Flaws in the service-to-service security model (token issuance/validation, scope enforcement).
- Path traversal or arbitrary file access via document storage.
- Secrets accidentally committed to the repository.
- Vulnerable dependencies not yet caught by Dependabot / CI.
- Anything in the docs that would lead someone to deploy the lab insecurely.

## Supported versions

Only the latest commit on `main` is supported.

## Reporting a vulnerability

Please **do not open a public issue**. Use GitHub's
[private vulnerability reporting](https://github.com/dalabe/AgentLab/security/advisories/new)
("Security" tab → "Report a vulnerability"). Include steps to reproduce, affected component and
impact. You can expect an acknowledgement within 7 days.

## Automated security checks

Every push and pull request runs CodeQL (SAST), Gitleaks (secrets), NuGet/npm vulnerability audits
and dependency review; a weekly DAST job runs auth smoke tests and an OWASP ZAP baseline scan.
See [ADR-0015](specs/adr/0015-ci-quality-and-security-gates.md).
