using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Procesos.BackOffice.Api.Dtos;
using Procesos.Data;
using Procesos.Data.Entities;

namespace Procesos.BackOffice.Api.Endpoints;

public static class EventosEndpoints
{
    public static void MapEventosEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/procesos/{procesoId:int}/eventos", ListarAsync).WithTags("Eventos");
        app.MapPost("/api/procesos/{procesoId:int}/eventos", CrearAsync).WithTags("Eventos");
        app.MapDelete("/api/eventos/{id:int}", EliminarAsync).WithTags("Eventos");
    }

    private static async Task<Results<Ok<List<EventoListItemDto>>, NotFound>> ListarAsync(ProcesosDbContext db, int procesoId)
    {
        if (!await db.Procesos.AnyAsync(p => p.Id == procesoId))
        {
            return TypedResults.NotFound();
        }

        var eventos = await db.Eventos.AsNoTracking()
            .Where(e => e.ProcesoId == procesoId)
            .OrderBy(e => e.Fecha)
            .Select(e => e.ToListItemDto())
            .ToListAsync();

        return TypedResults.Ok(eventos);
    }

    private static async Task<Results<Created<EventoListItemDto>, NotFound<string>>> CrearAsync(ProcesosDbContext db, int procesoId, EventoUpsertDto dto)
    {
        var proceso = await db.Procesos.FirstOrDefaultAsync(p => p.Id == procesoId);
        if (proceso is null)
        {
            return TypedResults.NotFound($"No existe un proceso con Id={procesoId}.");
        }

        var evento = new ProcesoEvento
        {
            ProcesoId = procesoId,
            Fecha = dto.Fecha,
            Tipo = dto.Tipo,
            Descripcion = dto.Descripcion.Trim(),
        };

        db.Eventos.Add(evento);
        proceso.FechaUltimaActualizacionUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/procesos/{procesoId}/eventos", evento.ToListItemDto());
    }

    private static async Task<Results<NoContent, NotFound>> EliminarAsync(ProcesosDbContext db, int id)
    {
        var evento = await db.Eventos.FirstOrDefaultAsync(e => e.Id == id);
        if (evento is null)
        {
            return TypedResults.NotFound();
        }

        db.Eventos.Remove(evento);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
