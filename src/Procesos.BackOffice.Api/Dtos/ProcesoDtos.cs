using Procesos.Data.Entities;

namespace Procesos.BackOffice.Api.Dtos;

public sealed record ProcesoListItemDto(
    int Id,
    string NumeroExpediente,
    string Tipo,
    string Estado,
    string EntidadOJuzgado,
    DateOnly FechaInicio,
    DateTime FechaUltimaActualizacionUtc,
    int PersonaId,
    string PersonaNombreCompleto);

public sealed record ProcesoDetailDto(
    int Id,
    string NumeroExpediente,
    string Tipo,
    string Estado,
    string EntidadOJuzgado,
    string Descripcion,
    DateOnly FechaInicio,
    DateTime FechaUltimaActualizacionUtc,
    string? Notas,
    PersonaListItemDto Persona,
    IReadOnlyList<DocumentoListItemDto> Documentos,
    IReadOnlyList<EventoListItemDto> Eventos);

public sealed record ProcesoUpsertDto(
    string NumeroExpediente,
    int PersonaId,
    TipoProceso Tipo,
    EstadoProceso Estado,
    string EntidadOJuzgado,
    string Descripcion,
    DateOnly FechaInicio,
    string? Notas);

public static class ProcesoMappingExtensions
{
    public static ProcesoListItemDto ToListItemDto(this Proceso p) => new(
        p.Id, p.NumeroExpediente, p.Tipo.ToString(), p.Estado.ToString(), p.EntidadOJuzgado,
        p.FechaInicio, p.FechaUltimaActualizacionUtc, p.PersonaId, p.Persona?.NombreCompleto ?? string.Empty);

    public static ProcesoDetailDto ToDetailDto(this Proceso p) => new(
        p.Id, p.NumeroExpediente, p.Tipo.ToString(), p.Estado.ToString(), p.EntidadOJuzgado, p.Descripcion,
        p.FechaInicio, p.FechaUltimaActualizacionUtc, p.Notas,
        p.Persona!.ToListItemDto(),
        p.Documentos.OrderBy(d => d.FechaCargaUtc).Select(d => d.ToListItemDto()).ToList(),
        p.Eventos.OrderBy(e => e.Fecha).Select(e => e.ToListItemDto()).ToList());

    public static void ApplyTo(this ProcesoUpsertDto dto, Proceso entity)
    {
        entity.NumeroExpediente = dto.NumeroExpediente.Trim();
        entity.PersonaId = dto.PersonaId;
        entity.Tipo = dto.Tipo;
        entity.Estado = dto.Estado;
        entity.EntidadOJuzgado = dto.EntidadOJuzgado.Trim();
        entity.Descripcion = dto.Descripcion.Trim();
        entity.FechaInicio = dto.FechaInicio;
        entity.Notas = dto.Notas?.Trim();
        entity.FechaUltimaActualizacionUtc = DateTime.UtcNow;
    }
}
