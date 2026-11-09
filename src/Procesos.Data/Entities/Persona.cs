namespace Procesos.Data.Entities;

/// <summary>
/// Persona (parte) involucrada en uno o más procesos legales.
/// </summary>
public class Persona
{
    public int Id { get; set; }

    /// <summary>Número de documento de identidad (cédula/DNI). Único.</summary>
    public string DocumentoIdentidad { get; set; } = string.Empty;

    public string Nombres { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Proceso> Procesos { get; set; } = new List<Proceso>();

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}
