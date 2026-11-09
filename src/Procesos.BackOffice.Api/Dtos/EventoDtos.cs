using Procesos.Data.Entities;

namespace Procesos.BackOffice.Api.Dtos;

public sealed record EventoListItemDto(int Id, int ProcesoId, DateOnly Fecha, string Tipo, string Descripcion);

public sealed record EventoUpsertDto(DateOnly Fecha, TipoEvento Tipo, string Descripcion);

public static class EventoMappingExtensions
{
    public static EventoListItemDto ToListItemDto(this ProcesoEvento e) => new(e.Id, e.ProcesoId, e.Fecha, e.Tipo.ToString(), e.Descripcion);
}
