# Data model (`Procesos.Data`)

Database: SQL Server LocalDB `ProcesosLegalesLab` (ADR-0007). ORM: EF Core 10, Fluent
configuration in `ProcesosDbContext.OnModelCreating`. Initial migration: `20261109075329_InitialCreate`.

## Entities

### `Persona` (table `Personas`)

| Column | Type | Constraints |
|---|---|---|
| `Id` | int | PK, identity |
| `DocumentoIdentidad` | nvarchar(32) | required, **unique** |
| `Nombres` | nvarchar(120) | required |
| `Apellidos` | nvarchar(120) | required |
| `Email` | nvarchar(200) | nullable |
| `Telefono` | nvarchar(30) | nullable |
| `FechaNacimiento` | date | nullable |
| `CreatedAtUtc` | datetime2 | required |

Computed in code (not mapped): `NombreCompleto = Nombres + " " + Apellidos`.

### `Proceso` (table `Procesos`)

| Column | Type | Constraints |
|---|---|---|
| `Id` | int | PK |
| `NumeroExpediente` | nvarchar(40) | required, **unique**, format `EXP-YYYY-NNNNN` (convention) |
| `PersonaId` | int | FK → Personas, cascade delete |
| `Tipo` | nvarchar(30) | `TipoProceso` as string |
| `Estado` | nvarchar(30) | `EstadoProceso` as string |
| `EntidadOJuzgado` | nvarchar(200) | required |
| `Descripcion` | nvarchar(2000) | required |
| `FechaInicio` | date | required |
| `FechaUltimaActualizacionUtc` | datetime2 | updated on document upload |
| `Notas` | nvarchar(2000) | nullable |

### `DocumentoProceso` (table `Documentos`)

| Column | Type | Constraints |
|---|---|---|
| `Id` | int | PK |
| `ProcesoId` | int | FK → Procesos, cascade delete |
| `Nombre` | nvarchar(200) | required |
| `Tipo` | nvarchar(30) | `TipoDocumento` as string |
| `RutaRelativa` | nvarchar(400) | required, relative to `DocumentStorage:RootPath`, never absolute |
| `ContentType` | nvarchar(100) | required, default `text/plain` |
| `FechaCargaUtc` | datetime2 | required |
| `TamanoBytes` | bigint | required |

### `ProcesoEvento` (table `Eventos`)

| Column | Type | Constraints |
|---|---|---|
| `Id` | int | PK |
| `ProcesoId` | int | FK → Procesos, cascade delete |
| `Fecha` | date | required |
| `Tipo` | nvarchar(30) | `TipoEvento` as string |
| `Descripcion` | nvarchar(500) | required |

## Enums (stored and serialised as strings)

| Enum | Values |
|---|---|
| `TipoProceso` | Civil, Penal, Laboral, Administrativo, Comercial, Familia |
| `EstadoProceso` | Activo, EnRevision, Suspendido, Cerrado, Archivado |
| `TipoDocumento` | Demanda, Contestacion, Sentencia, Contrato, Prueba, Resolucion, Notificacion, Otro |
| `TipoEvento` | Presentacion, Audiencia, Notificacion, Resolucion, Recurso, Pago, Otro |

Adding an enum value needs no migration (string column), but existing rows and the Angular
`models.ts` must be kept in sync.

## Physical documents

- Root: `DocumentStorage:RootPath`; default `<repo>/mock-data/documentos` via
  `RepoPaths.ResolveMockDocumentsRoot()` (searches upward for `ProcesosLegalesLab.slnx`).
- Layout: `<root>/<NumeroExpediente>/<file>`. Seeded files: `NN_Title.txt`; uploads: `{guid}_{originalName}`.
- Access only via `DocumentPathResolver.ResolveFullPath(relativePath)` (ADR-0010).

## Seed data (`DbSeeder.SeedAsync`)

Development only, idempotent (skips if `Personas` has rows). ~8 people, 14 cases
(`EXP-2022-00276` … `EXP-2026-00067`), 22 text documents written to disk, and timeline events.
All data is **synthetic**.

## Migrations

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add <Name> --project src/Procesos.Data
```

Design-time context: `ProcesosDbContextFactory`. Migrations are applied by BackOffice.Api at startup in Development.
