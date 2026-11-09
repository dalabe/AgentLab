namespace Procesos.Data.Entities;

/// <summary>Área legal del proceso.</summary>
public enum TipoProceso
{
    Civil,
    Penal,
    Laboral,
    Administrativo,
    Comercial,
    Familia,
}

/// <summary>Estado actual del proceso dentro de su ciclo de vida.</summary>
public enum EstadoProceso
{
    Activo,
    EnRevision,
    Suspendido,
    Cerrado,
    Archivado,
}

/// <summary>
/// Proceso legal (expediente) asociado a una <see cref="Persona"/>.
/// Es la entidad central que el chat MCP consulta.
/// </summary>
public class Proceso
{
    public int Id { get; set; }

    /// <summary>Identificador legible del expediente, p.ej. "EXP-2024-00031". Único.</summary>
    public string NumeroExpediente { get; set; } = string.Empty;

    public int PersonaId { get; set; }

    public Persona? Persona { get; set; }

    public TipoProceso Tipo { get; set; }

    public EstadoProceso Estado { get; set; }

    /// <summary>Juzgado, entidad o despacho a cargo del proceso.</summary>
    public string EntidadOJuzgado { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public DateOnly FechaInicio { get; set; }

    public DateTime FechaUltimaActualizacionUtc { get; set; }

    public string? Notas { get; set; }

    public ICollection<DocumentoProceso> Documentos { get; set; } = new List<DocumentoProceso>();

    public ICollection<ProcesoEvento> Eventos { get; set; } = new List<ProcesoEvento>();
}
