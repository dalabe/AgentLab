using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Procesos.BackOffice.Api.Dtos;
using Procesos.Data;
using Procesos.Data.Entities;
using Procesos.Data.Storage;

namespace Procesos.BackOffice.Api.Endpoints;

public static class DocumentosEndpoints
{
    public static void MapDocumentosEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/procesos/{procesoId:int}/documentos", ListarAsync).WithTags("Documentos");
        app.MapPost("/api/procesos/{procesoId:int}/documentos", SubirAsync).WithTags("Documentos")
            .DisableAntiforgery();
        app.MapGet("/api/documentos/{id:int}/contenido", DescargarContenidoAsync).WithTags("Documentos");
        app.MapDelete("/api/documentos/{id:int}", EliminarAsync).WithTags("Documentos");
    }

    private static async Task<Results<Ok<List<DocumentoListItemDto>>, NotFound>> ListarAsync(ProcesosDbContext db, int procesoId)
    {
        if (!await db.Procesos.AnyAsync(p => p.Id == procesoId))
        {
            return TypedResults.NotFound();
        }

        var documentos = await db.Documentos.AsNoTracking()
            .Where(d => d.ProcesoId == procesoId)
            .OrderBy(d => d.FechaCargaUtc)
            .Select(d => d.ToListItemDto())
            .ToListAsync();

        return TypedResults.Ok(documentos);
    }

    private static async Task<Results<Created<DocumentoListItemDto>, NotFound<string>, BadRequest<string>>> SubirAsync(
        ProcesosDbContext db, DocumentPathResolver pathResolver, int procesoId,
        IFormFile archivo, [FromForm] string nombre, [FromForm] TipoDocumento tipo)
    {
        var proceso = await db.Procesos.FirstOrDefaultAsync(p => p.Id == procesoId);
        if (proceso is null)
        {
            return TypedResults.NotFound($"No existe un proceso con Id={procesoId}.");
        }

        if (archivo.Length == 0)
        {
            return TypedResults.BadRequest("El archivo adjunto está vacío.");
        }

        var nombreArchivoSeguro = $"{Guid.NewGuid():N}_{Path.GetFileName(archivo.FileName)}";
        var rutaRelativa = Path.Combine(proceso.NumeroExpediente, nombreArchivoSeguro);
        var rutaFisica = pathResolver.ResolveFullPath(rutaRelativa);

        Directory.CreateDirectory(Path.GetDirectoryName(rutaFisica)!);

        await using (var destino = File.Create(rutaFisica))
        {
            await archivo.CopyToAsync(destino);
        }

        var documento = new DocumentoProceso
        {
            ProcesoId = procesoId,
            Nombre = string.IsNullOrWhiteSpace(nombre) ? archivo.FileName : nombre.Trim(),
            Tipo = tipo,
            RutaRelativa = rutaRelativa,
            ContentType = string.IsNullOrWhiteSpace(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType,
            FechaCargaUtc = DateTime.UtcNow,
            TamanoBytes = archivo.Length,
        };

        db.Documentos.Add(documento);
        proceso.FechaUltimaActualizacionUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/documentos/{documento.Id}/contenido", documento.ToListItemDto());
    }

    private static async Task<Results<PhysicalFileHttpResult, NotFound>> DescargarContenidoAsync(
        ProcesosDbContext db, DocumentPathResolver pathResolver, int id)
    {
        var documento = await db.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (documento is null)
        {
            return TypedResults.NotFound();
        }

        var rutaFisica = pathResolver.ResolveFullPath(documento.RutaRelativa);
        if (!File.Exists(rutaFisica))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.PhysicalFile(rutaFisica, documento.ContentType, documento.Nombre);
    }

    private static async Task<Results<NoContent, NotFound>> EliminarAsync(ProcesosDbContext db, DocumentPathResolver pathResolver, int id)
    {
        var documento = await db.Documentos.FirstOrDefaultAsync(d => d.Id == id);
        if (documento is null)
        {
            return TypedResults.NotFound();
        }

        var rutaFisica = pathResolver.ResolveFullPath(documento.RutaRelativa);

        db.Documentos.Remove(documento);
        await db.SaveChangesAsync();

        if (File.Exists(rutaFisica))
        {
            File.Delete(rutaFisica);
        }

        return TypedResults.NoContent();
    }
}
