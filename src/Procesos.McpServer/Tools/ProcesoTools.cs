using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Procesos.Data;
using Procesos.Data.Storage;

namespace Procesos.McpServer.Tools;

/// <summary>
/// Tools MCP para consultar personas y procesos legales. Son de solo lectura: el MCP Server
/// nunca modifica datos, solo los expone al LLM a través del protocolo MCP. Las altas/ediciones
/// se hacen exclusivamente desde el BackOffice (Procesos.BackOffice.Api).
/// </summary>
[McpServerToolType]
public sealed class ProcesoTools
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ProcesosDbContext _db;
    private readonly DocumentPathResolver _pathResolver;

    public ProcesoTools(ProcesosDbContext db, DocumentPathResolver pathResolver)
    {
        _db = db;
        _pathResolver = pathResolver;
    }

    [McpServerTool(Name = "buscar_personas")]
    [Description("Busca personas por nombre (coincidencia parcial, insensible a mayúsculas) o por número exacto de documento de identidad. Úsala primero cuando el usuario pregunte por procesos de alguien por nombre, para obtener su personaId.")]
    public async Task<string> BuscarPersonas(
        [Description("Nombre, apellido o fragmento del nombre a buscar; o número exacto de documento de identidad.")] string texto)
    {
        var textoNormalizado = texto.Trim();

        var resultados = await _db.Personas.AsNoTracking()
            .Where(p => p.DocumentoIdentidad == textoNormalizado
                || EF.Functions.Like(p.Nombres, $"%{textoNormalizado}%")
                || EF.Functions.Like(p.Apellidos, $"%{textoNormalizado}%")
                || EF.Functions.Like(p.Nombres + " " + p.Apellidos, $"%{textoNormalizado}%"))
            .OrderBy(p => p.Apellidos)
            .Take(20)
            .Select(p => new PersonaResumenDto(p.Id, p.Nombres + " " + p.Apellidos, p.DocumentoIdentidad, p.Email, p.Telefono))
            .ToListAsync();

        return JsonSerializer.Serialize(resultados, JsonOptions);
    }

    [McpServerTool(Name = "listar_procesos_de_persona")]
    [Description("Lista los procesos legales asociados a una persona, dado su personaId (obtenido con buscar_personas).")]
    public async Task<string> ListarProcesosDePersona(
        [Description("Id numérico de la persona.")] int personaId)
    {
        var persona = await _db.Personas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == personaId)
            ?? throw new McpException($"No existe ninguna persona con personaId={personaId}.");

        var procesos = await _db.Procesos.AsNoTracking()
            .Where(p => p.PersonaId == personaId)
            .OrderByDescending(p => p.FechaUltimaActualizacionUtc)
            .Select(p => new ProcesoResumenDto(p.NumeroExpediente, p.Tipo.ToString(), p.Estado.ToString(), p.EntidadOJuzgado, p.FechaInicio, p.FechaUltimaActualizacionUtc))
            .ToListAsync();

        return JsonSerializer.Serialize(new { persona = persona.NombreCompleto, procesos }, JsonOptions);
    }

    [McpServerTool(Name = "obtener_proceso")]
    [Description("Obtiene el detalle completo de un proceso legal por su número de expediente (p.ej. 'EXP-2024-00031'): persona asociada, estado, descripción, entidad/juzgado, documentos y línea de tiempo de eventos.")]
    public async Task<string> ObtenerProceso(
        [Description("Número de expediente exacto, p.ej. 'EXP-2024-00031'.")] string numeroExpediente)
    {
        var dto = await CargarProcesoDetalle(numeroExpediente);
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    [McpServerTool(Name = "listar_documentos_de_proceso")]
    [Description("Lista los documentos (metadatos, sin contenido) asociados a un proceso legal por su número de expediente.")]
    public async Task<string> ListarDocumentosDeProceso(
        [Description("Número de expediente exacto, p.ej. 'EXP-2024-00031'.")] string numeroExpediente)
    {
        var proceso = await _db.Procesos.AsNoTracking().FirstOrDefaultAsync(p => p.NumeroExpediente == numeroExpediente)
            ?? throw new McpException($"No existe ningún proceso con número de expediente '{numeroExpediente}'.");

        var documentos = await _db.Documentos.AsNoTracking()
            .Where(d => d.ProcesoId == proceso.Id)
            .OrderBy(d => d.FechaCargaUtc)
            .Select(d => new DocumentoResumenDto(d.Id, d.Nombre, d.Tipo.ToString(), d.FechaCargaUtc, d.TamanoBytes))
            .ToListAsync();

        return JsonSerializer.Serialize(documentos, JsonOptions);
    }

    [McpServerTool(Name = "listar_eventos_proceso")]
    [Description("Lista la línea de tiempo de movimientos (audiencias, notificaciones, resoluciones, etc.) de un proceso legal por su número de expediente, ordenados cronológicamente.")]
    public async Task<string> ListarEventosProceso(
        [Description("Número de expediente exacto, p.ej. 'EXP-2024-00031'.")] string numeroExpediente)
    {
        var proceso = await _db.Procesos.AsNoTracking().FirstOrDefaultAsync(p => p.NumeroExpediente == numeroExpediente)
            ?? throw new McpException($"No existe ningún proceso con número de expediente '{numeroExpediente}'.");

        var eventos = await _db.Eventos.AsNoTracking()
            .Where(e => e.ProcesoId == proceso.Id)
            .OrderBy(e => e.Fecha)
            .Select(e => new EventoDto(e.Fecha, e.Tipo.ToString(), e.Descripcion))
            .ToListAsync();

        return JsonSerializer.Serialize(eventos, JsonOptions);
    }

    [McpServerTool(Name = "obtener_contenido_documento")]
    [Description("Obtiene el contenido de texto de un documento específico de un proceso, para poder citarlo textualmente. Requiere el número de expediente y el documentoId (obtenido con listar_documentos_de_proceso).")]
    public async Task<string> ObtenerContenidoDocumento(
        [Description("Número de expediente exacto, p.ej. 'EXP-2024-00031'.")] string numeroExpediente,
        [Description("Id numérico del documento dentro de ese proceso.")] int documentoId)
    {
        var proceso = await _db.Procesos.AsNoTracking().FirstOrDefaultAsync(p => p.NumeroExpediente == numeroExpediente)
            ?? throw new McpException($"No existe ningún proceso con número de expediente '{numeroExpediente}'.");

        var documento = await _db.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentoId && d.ProcesoId == proceso.Id)
            ?? throw new McpException($"El proceso '{numeroExpediente}' no tiene un documento con documentoId={documentoId}.");

        string fullPath;
        try
        {
            fullPath = _pathResolver.ResolveFullPath(documento.RutaRelativa);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new McpException("Ruta de documento inválida.", ex);
        }

        if (!File.Exists(fullPath))
        {
            throw new McpException($"El archivo físico del documento '{documento.Nombre}' no se encontró en el almacenamiento de documentos.");
        }

        return await File.ReadAllTextAsync(fullPath);
    }

    private async Task<ProcesoDetalleDto> CargarProcesoDetalle(string numeroExpediente)
    {
        var proceso = await _db.Procesos.AsNoTracking()
            .Include(p => p.Persona)
            .Include(p => p.Documentos)
            .Include(p => p.Eventos)
            .FirstOrDefaultAsync(p => p.NumeroExpediente == numeroExpediente)
            ?? throw new McpException($"No existe ningún proceso con número de expediente '{numeroExpediente}'.");

        var persona = proceso.Persona!;

        return new ProcesoDetalleDto(
            proceso.NumeroExpediente,
            proceso.Tipo.ToString(),
            proceso.Estado.ToString(),
            proceso.EntidadOJuzgado,
            proceso.Descripcion,
            proceso.FechaInicio,
            proceso.FechaUltimaActualizacionUtc,
            proceso.Notas,
            new PersonaResumenDto(persona.Id, persona.NombreCompleto, persona.DocumentoIdentidad, persona.Email, persona.Telefono),
            proceso.Documentos
                .OrderBy(d => d.FechaCargaUtc)
                .Select(d => new DocumentoResumenDto(d.Id, d.Nombre, d.Tipo.ToString(), d.FechaCargaUtc, d.TamanoBytes))
                .ToList(),
            proceso.Eventos
                .OrderBy(e => e.Fecha)
                .Select(e => new EventoDto(e.Fecha, e.Tipo.ToString(), e.Descripcion))
                .ToList());
    }
}
