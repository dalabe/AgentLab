namespace Procesos.McpServer.Tools;

public sealed record PersonaResumenDto(int PersonaId, string NombreCompleto, string DocumentoIdentidad, string? Email, string? Telefono);

public sealed record DocumentoResumenDto(int DocumentoId, string Nombre, string Tipo, DateTime FechaCargaUtc, long TamanoBytes);

public sealed record EventoDto(DateOnly Fecha, string Tipo, string Descripcion);

public sealed record ProcesoResumenDto(string NumeroExpediente, string Tipo, string Estado, string EntidadOJuzgado, DateOnly FechaInicio, DateTime FechaUltimaActualizacionUtc);

public sealed record ProcesoDetalleDto(
    string NumeroExpediente,
    string Tipo,
    string Estado,
    string EntidadOJuzgado,
    string Descripcion,
    DateOnly FechaInicio,
    DateTime FechaUltimaActualizacionUtc,
    string? Notas,
    PersonaResumenDto Persona,
    IReadOnlyList<DocumentoResumenDto> Documentos,
    IReadOnlyList<EventoDto> Eventos);
