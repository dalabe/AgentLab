using Procesos.Data.Entities;

namespace Procesos.BackOffice.Api.Dtos;

public sealed record PersonaListItemDto(
    int Id,
    string NombreCompleto,
    string DocumentoIdentidad,
    string? Email,
    string? Telefono,
    int CantidadProcesos);

public sealed record PersonaDetailDto(
    int Id,
    string Nombres,
    string Apellidos,
    string DocumentoIdentidad,
    string? Email,
    string? Telefono,
    DateOnly? FechaNacimiento,
    DateTime CreatedAtUtc,
    IReadOnlyList<ProcesoListItemDto> Procesos);

public sealed record PersonaUpsertDto(
    string DocumentoIdentidad,
    string Nombres,
    string Apellidos,
    string? Email,
    string? Telefono,
    DateOnly? FechaNacimiento);

public static class PersonaMappingExtensions
{
    public static PersonaListItemDto ToListItemDto(this Persona p) => new(
        p.Id, p.NombreCompleto, p.DocumentoIdentidad, p.Email, p.Telefono, p.Procesos.Count);

    public static PersonaDetailDto ToDetailDto(this Persona p) => new(
        p.Id, p.Nombres, p.Apellidos, p.DocumentoIdentidad, p.Email, p.Telefono, p.FechaNacimiento, p.CreatedAtUtc,
        p.Procesos.OrderByDescending(x => x.FechaUltimaActualizacionUtc).Select(x => x.ToListItemDto()).ToList());

    public static void ApplyTo(this PersonaUpsertDto dto, Persona entity)
    {
        entity.DocumentoIdentidad = dto.DocumentoIdentidad.Trim();
        entity.Nombres = dto.Nombres.Trim();
        entity.Apellidos = dto.Apellidos.Trim();
        entity.Email = dto.Email?.Trim();
        entity.Telefono = dto.Telefono?.Trim();
        entity.FechaNacimiento = dto.FechaNacimiento;
    }
}
