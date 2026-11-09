using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Procesos.BackOffice.Api.Dtos;
using Procesos.Data;
using Procesos.Data.Entities;

namespace Procesos.BackOffice.Api.Endpoints;

public static class ProcesosEndpoints
{
    public static RouteGroupBuilder MapProcesosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/procesos").WithTags("Procesos");

        group.MapGet("/", ListarAsync);
        group.MapGet("/{id:int}", ObtenerAsync);
        group.MapGet("/by-expediente/{numeroExpediente}", ObtenerPorExpedienteAsync);
        group.MapPost("/", CrearAsync);
        group.MapPut("/{id:int}", ActualizarAsync);
        group.MapDelete("/{id:int}", EliminarAsync);

        return group;
    }

    private static async Task<Ok<List<ProcesoListItemDto>>> ListarAsync(
        ProcesosDbContext db, int? personaId, TipoProceso? tipo, EstadoProceso? estado, string? q)
    {
        var query = db.Procesos.AsNoTracking().Include(p => p.Persona).AsQueryable();

        if (personaId is not null) query = query.Where(p => p.PersonaId == personaId);
        if (tipo is not null) query = query.Where(p => p.Tipo == tipo);
        if (estado is not null) query = query.Where(p => p.Estado == estado);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var texto = q.Trim();
            query = query.Where(p => p.NumeroExpediente.Contains(texto) || p.Descripcion.Contains(texto));
        }

        var procesos = await query
            .OrderByDescending(p => p.FechaUltimaActualizacionUtc)
            .Select(p => p.ToListItemDto())
            .ToListAsync();

        return TypedResults.Ok(procesos);
    }

    private static async Task<Results<Ok<ProcesoDetailDto>, NotFound>> ObtenerAsync(ProcesosDbContext db, int id)
    {
        var proceso = await CargarDetalleQuery(db).FirstOrDefaultAsync(p => p.Id == id);
        return proceso is null ? TypedResults.NotFound() : TypedResults.Ok(proceso.ToDetailDto());
    }

    private static async Task<Results<Ok<ProcesoDetailDto>, NotFound>> ObtenerPorExpedienteAsync(ProcesosDbContext db, string numeroExpediente)
    {
        var proceso = await CargarDetalleQuery(db).FirstOrDefaultAsync(p => p.NumeroExpediente == numeroExpediente);
        return proceso is null ? TypedResults.NotFound() : TypedResults.Ok(proceso.ToDetailDto());
    }

    private static async Task<Results<Created<ProcesoDetailDto>, NotFound<string>, Conflict<string>>> CrearAsync(ProcesosDbContext db, ProcesoUpsertDto dto)
    {
        if (!await db.Personas.AnyAsync(p => p.Id == dto.PersonaId))
        {
            return TypedResults.NotFound($"No existe una persona con Id={dto.PersonaId}.");
        }

        if (await db.Procesos.AnyAsync(p => p.NumeroExpediente == dto.NumeroExpediente.Trim()))
        {
            return TypedResults.Conflict($"Ya existe un proceso con número de expediente '{dto.NumeroExpediente}'.");
        }

        var proceso = new Proceso();
        dto.ApplyTo(proceso);

        db.Procesos.Add(proceso);
        await db.SaveChangesAsync();

        var creado = await CargarDetalleQuery(db).FirstAsync(p => p.Id == proceso.Id);
        return TypedResults.Created($"/api/procesos/{proceso.Id}", creado.ToDetailDto());
    }

    private static async Task<Results<Ok<ProcesoDetailDto>, NotFound, Conflict<string>>> ActualizarAsync(ProcesosDbContext db, int id, ProcesoUpsertDto dto)
    {
        var proceso = await db.Procesos.FirstOrDefaultAsync(p => p.Id == id);
        if (proceso is null)
        {
            return TypedResults.NotFound();
        }

        var expedienteEnUso = await db.Procesos.AnyAsync(p => p.Id != id && p.NumeroExpediente == dto.NumeroExpediente.Trim());
        if (expedienteEnUso)
        {
            return TypedResults.Conflict($"Ya existe otro proceso con número de expediente '{dto.NumeroExpediente}'.");
        }

        dto.ApplyTo(proceso);
        await db.SaveChangesAsync();

        var actualizado = await CargarDetalleQuery(db).FirstAsync(p => p.Id == id);
        return TypedResults.Ok(actualizado.ToDetailDto());
    }

    private static async Task<Results<NoContent, NotFound>> EliminarAsync(ProcesosDbContext db, int id)
    {
        var proceso = await db.Procesos.FirstOrDefaultAsync(p => p.Id == id);
        if (proceso is null)
        {
            return TypedResults.NotFound();
        }

        db.Procesos.Remove(proceso); // cascada: elimina también sus documentos/eventos
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static IQueryable<Proceso> CargarDetalleQuery(ProcesosDbContext db) =>
        db.Procesos.AsNoTracking()
            .Include(p => p.Persona)
            .Include(p => p.Documentos)
            .Include(p => p.Eventos);
}
