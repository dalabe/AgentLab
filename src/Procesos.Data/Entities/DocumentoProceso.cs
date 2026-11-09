namespace Procesos.Data.Entities;

/// <summary>Clasificación del documento dentro del expediente.</summary>
public enum TipoDocumento
{
    Demanda,
    Contestacion,
    Sentencia,
    Contrato,
    Prueba,
    Resolucion,
    Notificacion,
    Otro,
}

/// <summary>
/// Documento físico referenciado por un <see cref="Proceso"/>. El archivo real vive bajo la
/// carpeta de almacenamiento configurada (ver <c>DocumentStorageOptions</c>); esta entidad solo
/// guarda la ruta relativa y metadatos, nunca el contenido binario.
/// </summary>
public class DocumentoProceso
{
    public int Id { get; set; }

    public int ProcesoId { get; set; }

    public Proceso? Proceso { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public TipoDocumento Tipo { get; set; }

    /// <summary>
    /// Ruta relativa a la raíz de almacenamiento de documentos (nunca una ruta absoluta ni con
    /// segmentos "..": se valida al resolverla con <c>DocumentPathResolver</c>).
    /// </summary>
    public string RutaRelativa { get; set; } = string.Empty;

    public string ContentType { get; set; } = "text/plain";

    public DateTime FechaCargaUtc { get; set; }

    public long TamanoBytes { get; set; }
}
