# ADR-0010: Store document binaries on the filesystem behind a path-traversal guard

- **Status**: Accepted
- **Date**: 2026-11-09
- **Tags**: data, security

## Context

Cases have physical documents that the chat must quote and the BackOffice must upload/download.
Blobs in SQL bloat the DB; paths built from user/DB input risk path traversal.

## Decision

- Files live under `DocumentStorage:RootPath` (default `<repo>/mock-data/documentos`, located by
  `RepoPaths.FindRepoRoot()`); the DB stores only `RutaRelativa` + metadata.
- **All** file access goes through `DocumentPathResolver.ResolveFullPath()`, which normalises the
  path and throws `UnauthorizedAccessException` if the result escapes the root.
- Uploads are saved as `{Guid}_{Path.GetFileName(original)}` under the case-number folder.

## Consequences

- Negative: no content-type allow-list or explicit size cap on upload yet (`docs/SECURITY.md` T5).
- Follow-up: for cloud deployment, replace with blob storage behind the same resolver abstraction.

## References

- `src/Procesos.Data/Storage/DocumentPathResolver.cs`, `src/Procesos.BackOffice.Api/Endpoints/DocumentosEndpoints.cs`
- Tests: `tests/Procesos.UnitTests/DocumentPathResolverTests.cs`
