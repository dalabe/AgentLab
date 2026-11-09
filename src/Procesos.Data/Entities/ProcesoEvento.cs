namespace Procesos.Data.Entities;

/// <summary>Tipo de movimiento en la línea de tiempo de un proceso.</summary>
public enum TipoEvento
{
    Presentacion,
    Audiencia,
    Notificacion,
    Resolucion,
    Recurso,
    Pago,
    Otro,
}

/// <summary>
/// Movimiento puntual en la línea de tiempo de un <see cref="Proceso"/> (p.ej. una audiencia,
/// una notificación, una resolución). Permite responder preguntas como
/// "¿cuál fue el último movimiento del proceso X?".
/// </summary>
public class ProcesoEvento
{
    public int Id { get; set; }

    public int ProcesoId { get; set; }

    public Proceso? Proceso { get; set; }

    public DateOnly Fecha { get; set; }

    public TipoEvento Tipo { get; set; }

    public string Descripcion { get; set; } = string.Empty;
}
