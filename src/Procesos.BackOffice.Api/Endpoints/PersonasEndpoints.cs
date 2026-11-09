using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Procesos.BackOffice.Api.Dtos;
using Procesos.Data;
using Procesos.Data.Entities;

namespace Procesos.BackOffice.Api.Endpoints;

public static class PersonasEndpoints
{
    public static RouteGroupBuilder MapPersonasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/personas").WithTags("Personas");

        group.MapGet("/", ListarAsync);
        group.MapGet("/{id:int}", ObtenerAsync);
        group.MapPost("/", CrearAsync);
        group.MapPut("/{id:int}", ActualizarAsync);
        group.MapDelete("/{id:int}", EliminarAsync);

        return group;
    }

    private static async Task<Ok<List<PersonaListItemDto>>> ListarAsync(ProcesosDbContext db, string? q)
    {
        var query = db.Personas.AsNoTracking().Include(p => p.Procesos).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var texto = q.Trim();
            query = query.Where(p => p.DocumentoIdentidad.Contains(texto)
                || p.Nombres.Contains(texto)
                || p.Apellidos.Contains(texto));
        }

        var personas = await query
            .OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres)
            .Select(p => p.ToListItemDto())
            .ToListAsync();

        return TypedResults.Ok(personas);
    }

    private static async Task<Results<Ok<PersonaDetailDto>, NotFound>> ObtenerAsync(ProcesosDbContext db, int id)
    {
        var persona = await db.Personas.AsNoTracking()
            .Include(p => p.Procesos)
            .FirstOrDefaultAsync(p => p.Id == id);

        return persona is null ? TypedResults.NotFound() : TypedResults.Ok(persona.ToDetailDto());
    }

    private static async Task<Results<Created<PersonaDetailDto>, Conflict<string>>> CrearAsync(ProcesosDbContext db, PersonaUpsertDto dto)
    {
        if (await db.Personas.AnyAsync(p => p.DocumentoIdentidad == dto.DocumentoIdentidad.Trim()))
        {
            return TypedResults.Conflict($"Ya existe una persona con documento de identidad '{dto.DocumentoIdentidad}'.");
        }

        var persona = new Persona { CreatedAtUtc = DateTime.UtcNow };
        dto.ApplyTo(persona);

        db.Personas.Add(persona);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/personas/{persona.Id}", persona.ToDetailDto());
    }

    private static async Task<Results<Ok<PersonaDetailDto>, NotFound, Conflict<string>>> ActualizarAsync(ProcesosDbContext db, int id, PersonaUpsertDto dto)
    {
        var persona = await db.Personas.Include(p => p.Procesos).FirstOrDefaultAsync(p => p.Id == id);
        if (persona is null)
        {
            return TypedResults.NotFound();
        }

        var documentoEnUso = await db.Personas.AnyAsync(p => p.Id != id && p.DocumentoIdentidad == dto.DocumentoIdentidad.Trim());
        if (documentoEnUso)
        {
            return TypedResults.Conflict($"Ya existe otra persona con documento de identidad '{dto.DocumentoIdentidad}'.");
        }

        dto.ApplyTo(persona);
        await db.SaveChangesAsync();

        return TypedResults.Ok(persona.ToDetailDto());
    }

    private static async Task<Results<NoContent, NotFound>> EliminarAsync(ProcesosDbContext db, int id)
    {
        var persona = await db.Personas.FirstOrDefaultAsync(p => p.Id == id);
        if (persona is null)
        {
            return TypedResults.NotFound();
        }

        db.Personas.Remove(persona); // cascada: elimina también sus procesos/documentos/eventos
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
