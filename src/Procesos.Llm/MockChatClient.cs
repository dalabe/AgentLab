using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace Procesos.Llm;

/// <summary>
/// Implementación de <see cref="IChatClient"/> sin LLM real: útil para ejecutar y probar todo el
/// laboratorio (chat → ChatApi → MCP Client → MCP Server → SQL/archivos) sin necesitar una
/// API key de Anthropic. Usa heurísticas simples (regex) sobre el último mensaje del usuario para
/// decidir qué tool MCP invocar, y arma una respuesta de texto a partir del resultado.
/// No es un sustituto real de un LLM: es un modo "offline" determinista para demos y pruebas.
///
/// Igual que haría el middleware `UseFunctionInvocation` con un LLM real, deja constancia de las
/// tools invocadas como <see cref="FunctionCallContent"/>/<see cref="FunctionResultContent"/> en
/// el mensaje de respuesta, para que el panel "qué hizo" de la UI funcione también en este modo.
/// </summary>
public sealed partial class MockChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => GetResponseCoreAsync(messages, options, cancellationToken);

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseCoreAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
        // Nada que liberar: no mantiene conexiones ni recursos propios.
    }

    private static async Task<ChatResponse> GetResponseCoreAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
    {
        var ultimoMensaje = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
        var tools = options?.Tools?.OfType<AIFunction>().ToList() ?? [];

        var contenidos = new List<AIContent>();
        var texto = await ResolverRespuestaAsync(ultimoMensaje, tools, contenidos, cancellationToken);
        contenidos.Add(new TextContent(texto));

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, contenidos));
    }

    private static async Task<string> ResolverRespuestaAsync(string mensaje, List<AIFunction> tools, List<AIContent> contenidos, CancellationToken cancellationToken)
    {
        var expedienteMatch = ExpedienteRegex().Match(mensaje);
        if (expedienteMatch.Success)
        {
            return await ResponderPorExpedienteAsync(expedienteMatch.Value.ToUpperInvariant(), tools, contenidos, cancellationToken);
        }

        var nombreMatch = NombreRegex().Match(mensaje);
        if (nombreMatch.Success)
        {
            return await ResponderPorNombreAsync(nombreMatch.Value.Trim(), tools, contenidos, cancellationToken);
        }

        return MensajeAyuda(tools);
    }

    private static async Task<string> ResponderPorExpedienteAsync(string numeroExpediente, List<AIFunction> tools, List<AIContent> contenidos, CancellationToken ct)
    {
        var tool = BuscarTool(tools, "obtener_proceso");
        if (tool is null)
        {
            return "[Modo Mock] No encuentro la tool 'obtener_proceso' entre las herramientas disponibles.";
        }

        var resultado = await InvocarAsync(tool, new Dictionary<string, object?> { ["numeroExpediente"] = numeroExpediente }, contenidos, ct);
        if (resultado.Error is not null)
        {
            return $"[Modo Mock] No pude obtener el proceso '{numeroExpediente}': {resultado.Error}";
        }

        using var doc = JsonDocument.Parse(resultado.Json!);
        var root = doc.RootElement;

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"[Modo Mock] Proceso {root.GetProperty("numeroExpediente").GetString()} ({root.GetProperty("tipo").GetString()}):");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Estado: {root.GetProperty("estado").GetString()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Entidad/Juzgado: {root.GetProperty("entidadOJuzgado").GetString()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Parte: {root.GetProperty("persona").GetProperty("nombreCompleto").GetString()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Descripción: {root.GetProperty("descripcion").GetString()}");

        if (root.TryGetProperty("eventos", out var eventos) && eventos.GetArrayLength() > 0)
        {
            var ultimo = eventos[eventos.GetArrayLength() - 1];
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Último movimiento ({ultimo.GetProperty("fecha").GetString()}): {ultimo.GetProperty("descripcion").GetString()}");
        }

        return sb.ToString().TrimEnd();
    }

    private static async Task<string> ResponderPorNombreAsync(string nombre, List<AIFunction> tools, List<AIContent> contenidos, CancellationToken ct)
    {
        var buscarPersonas = BuscarTool(tools, "buscar_personas");
        var listarProcesos = BuscarTool(tools, "listar_procesos_de_persona");
        if (buscarPersonas is null || listarProcesos is null)
        {
            return "[Modo Mock] Faltan las tools 'buscar_personas' o 'listar_procesos_de_persona'.";
        }

        var busqueda = await InvocarAsync(buscarPersonas, new Dictionary<string, object?> { ["texto"] = nombre }, contenidos, ct);
        if (busqueda.Error is not null)
        {
            return $"[Modo Mock] Error buscando '{nombre}': {busqueda.Error}";
        }

        using var doc = JsonDocument.Parse(busqueda.Json!);
        var personas = doc.RootElement;

        if (personas.GetArrayLength() == 0)
        {
            return $"[Modo Mock] No encontré ninguna persona que coincida con '{nombre}'.";
        }

        var primera = personas[0];
        var personaId = primera.GetProperty("personaId").GetInt32();
        var nombreCompleto = primera.GetProperty("nombreCompleto").GetString();

        var procesosResultado = await InvocarAsync(listarProcesos, new Dictionary<string, object?> { ["personaId"] = personaId }, contenidos, ct);
        if (procesosResultado.Error is not null)
        {
            return $"[Modo Mock] Error listando procesos de '{nombreCompleto}': {procesosResultado.Error}";
        }

        using var docProcesos = JsonDocument.Parse(procesosResultado.Json!);
        var procesos = docProcesos.RootElement.GetProperty("procesos");

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"[Modo Mock] Procesos de {nombreCompleto}:");
        if (procesos.GetArrayLength() == 0)
        {
            sb.AppendLine("- No tiene procesos registrados.");
        }
        else
        {
            foreach (var p in procesos.EnumerateArray())
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"- {p.GetProperty("numeroExpediente").GetString()} ({p.GetProperty("tipo").GetString()}, {p.GetProperty("estado").GetString()})");
            }
        }

        if (personas.GetArrayLength() > 1)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"(Nota: la búsqueda encontró {personas.GetArrayLength()} coincidencias; se muestra la primera.)");
        }

        return sb.ToString().TrimEnd();
    }

    private static string MensajeAyuda(List<AIFunction> tools) =>
        "[Modo Mock - sin LLM real configurado] Puedo responder dos tipos de preguntas de forma determinista:\n" +
        "1) Preguntas con un número de expediente, p.ej. '¿Cuál es el estado del proceso EXP-2024-00031?'\n" +
        "2) Preguntas con el nombre completo de una persona, p.ej. '¿Qué procesos tiene Juan Carlos Pérez Londoño?'\n" +
        $"Tools MCP disponibles: {string.Join(", ", tools.Select(t => t.Name))}.\n" +
        "Para respuestas en lenguaje libre, configure Llm:Provider=Anthropic y una API key válida (ver docs/RUNBOOK.md).";

    private static AIFunction? BuscarTool(List<AIFunction> tools, string name) =>
        tools.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

    private static async Task<(string? Json, string? Error)> InvocarAsync(
        AIFunction tool, Dictionary<string, object?> args, List<AIContent> contenidos, CancellationToken ct)
    {
        var callId = Guid.NewGuid().ToString("N");
        contenidos.Add(new FunctionCallContent(callId, tool.Name, args!));

        try
        {
            var resultado = await tool.InvokeAsync(new AIFunctionArguments(args), ct);
            contenidos.Add(new FunctionResultContent(callId, resultado));
            return (resultado?.ToString(), null);
        }
        catch (Exception ex)
        {
            contenidos.Add(new FunctionResultContent(callId, $"Error: {ex.Message}"));
            return (null, ex.Message);
        }
    }

    [GeneratedRegex(@"EXP-\d{4}-\d+", RegexOptions.IgnoreCase)]
    private static partial Regex ExpedienteRegex();

    [GeneratedRegex(@"(?:[A-ZÁÉÍÓÚÑ][a-záéíóúñ]+\s+){1,4}[A-ZÁÉÍÓÚÑ][a-záéíóúñ]+")]
    private static partial Regex NombreRegex();
}
