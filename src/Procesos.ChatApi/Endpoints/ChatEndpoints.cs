using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.AI;
using Procesos.ChatApi.Dtos;
using Procesos.ChatApi.Sessions;
using Procesos.McpClientCore;

namespace Procesos.ChatApi.Endpoints;

public static class ChatEndpoints
{
    private const string SystemInstructions =
        """
        Eres un asistente legal interno que responde preguntas en español sobre personas y sus
        procesos legales (expedientes). Toda la información real vive detrás de las tools MCP
        disponibles: SIEMPRE que la pregunta se refiera a una persona o a un proceso concreto,
        usa las tools para consultar los datos reales antes de responder; nunca inventes números
        de expediente, estados, fechas ni contenido de documentos.

        Si el usuario menciona un número de expediente, úsalo directamente con la tool
        'obtener_proceso'. Si menciona el nombre de una persona, primero usa 'buscar_personas'
        para encontrar su personaId y luego 'listar_procesos_de_persona'. Si preguntan por el
        contenido de un documento, usa 'listar_documentos_de_proceso' para encontrar su
        documentoId y luego 'obtener_contenido_documento'.

        Si no encuentras la persona o el proceso, dilo explícitamente en vez de inventar datos.
        Responde de forma clara y concisa, citando el número de expediente cuando sea relevante.
        """;

    public static RouteGroupBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat").WithTags("Chat");

        group.MapPost("/messages", EnviarMensajeAsync);

        return group;
    }

    private static async Task<Results<Ok<ChatResponseDto>, BadRequest<string>>> EnviarMensajeAsync(
        ChatRequestDto request,
        IChatClient chatClient,
        IProcesosMcpClientAccessor mcpAccessor,
        ChatSessionStore sessionStore,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return TypedResults.BadRequest("El mensaje no puede estar vacío.");
        }

        var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId;
        var history = sessionStore.GetOrCreate(sessionId);

        history.Add(new ChatMessage(ChatRole.User, request.Message));

        var mcpClient = await mcpAccessor.GetClientAsync(cancellationToken);
        var tools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

        var options = new ChatOptions
        {
            Tools = [.. tools],
            Instructions = SystemInstructions,
            MaxOutputTokens = 1500,
        };

        var response = await chatClient.GetResponseAsync(history, options, cancellationToken);

        history.AddRange(response.Messages);

        var toolCalls = ExtraerToolCalls(response.Messages);

        return TypedResults.Ok(new ChatResponseDto(sessionId, response.Text, toolCalls));
    }

    /// <summary>
    /// Reconstruye, a partir de los mensajes intermedios que deja <c>UseFunctionInvocation</c>,
    /// qué tools se invocaron y con qué resultado. Es solo para transparencia en la UI (panel
    /// "qué hizo"); el modelo ya vio y usó estos resultados para construir su respuesta final.
    /// </summary>
    private static List<ToolCallDto> ExtraerToolCalls(IEnumerable<ChatMessage> messages)
    {
        var acumulado = new Dictionary<string, ToolCallAccumulator>();

        foreach (var content in messages.SelectMany(m => m.Contents))
        {
            switch (content)
            {
                case FunctionCallContent call:
                    acumulado[call.CallId] = new ToolCallAccumulator(call.Name, call.Arguments, null);
                    break;
                case FunctionResultContent result when acumulado.TryGetValue(result.CallId, out var existente):
                    acumulado[result.CallId] = existente with { Result = result.Result };
                    break;
            }
        }

        return acumulado.Values
            .Select(a => new ToolCallDto(a.Name, a.Arguments, a.Result?.ToString()))
            .ToList();
    }

    private sealed record ToolCallAccumulator(string Name, IDictionary<string, object?>? Arguments, object? Result);
}
