using Procesos.Data.Entities;

namespace Procesos.BackOffice.Api.Dtos;

public sealed record DocumentoListItemDto(
    int Id,
    int ProcesoId,
    string Nombre,
    string Tipo,
    string ContentType,
    DateTime FechaCargaUtc,
    long TamanoBytes);

/// <summary>Metadatos para registrar un documento nuevo; el contenido va en un archivo adjunto (multipart/form-data).</summary>
public sealed record DocumentoUploadMetadataDto(string Nombre, TipoDocumento Tipo);

public static class DocumentoMappingExtensions
{
    public static DocumentoListItemDto ToListItemDto(this DocumentoProceso d) => new(
        d.Id, d.ProcesoId, d.Nombre, d.Tipo.ToString(), d.ContentType, d.FechaCargaUtc, d.TamanoBytes);
}
