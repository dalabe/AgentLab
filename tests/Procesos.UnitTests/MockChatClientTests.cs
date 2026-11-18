using Microsoft.Extensions.AI;
using Procesos.Llm;

namespace Procesos.UnitTests;

/// <summary>Deterministic offline LLM used when no API key is configured (ADR-0006, NFR-07).</summary>
public sealed class MockChatClientTests
{
    private const string ProcesoJson = """
        {
          "numeroExpediente": "EXP-2024-00031", "tipo": "Laboral", "estado": "Activo",
          "entidadOJuzgado": "Juzgado 3 Laboral", "descripcion": "Despido sin justa causa",
          "persona": { "personaId": 1, "nombreCompleto": "Ana Gómez" },
          "eventos": [ { "fecha": "2024-03-01", "tipo": "Audiencia", "descripcion": "Audiencia inicial" } ]
        }
        """;

    private static ChatMessage[] Ask(string text) => [new ChatMessage(ChatRole.User, text)];

    [Fact]
    public async Task CaseNumberQuestion_InvokesObtenerProceso_AndSummarisesResult()
    {
        string? receivedArgument = null;
        var tool = AIFunctionFactory.Create(
            (string numeroExpediente) => { receivedArgument = numeroExpediente; return ProcesoJson; },
            "obtener_proceso");

        using var client = new MockChatClient();
        var response = await client.GetResponseAsync(
            Ask("¿Cuál es el estado del proceso exp-2024-00031?"),
            new ChatOptions { Tools = [tool] });

        Assert.Equal("EXP-2024-00031", receivedArgument);
        Assert.Contains("Activo", response.Text, StringComparison.Ordinal);
        Assert.Contains("Audiencia inicial", response.Text, StringComparison.Ordinal);

        var call = Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>());
        Assert.Equal("obtener_proceso", call.Name);
        Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>(), r => r.CallId == call.CallId);
    }

    [Fact]
    public async Task CaseNumberQuestion_WhenToolReportsError_ExplainsInsteadOfThrowing()
    {
        // Shape returned by the MCP client when the server answers a tools/call with isError=true.
        const string errorResult = """
            { "content": [ { "type": "text", "text": "No existe ningún proceso con número de expediente 'EXP-9999-00000'." } ], "isError": true }
            """;
        var tool = AIFunctionFactory.Create((string numeroExpediente) => errorResult, "obtener_proceso");

        using var client = new MockChatClient();
        var response = await client.GetResponseAsync(
            Ask("¿Cuál es el estado del proceso EXP-9999-00000?"),
            new ChatOptions { Tools = [tool] });

        Assert.Contains("No pude obtener el proceso 'EXP-9999-00000'", response.Text, StringComparison.Ordinal);
        Assert.Contains("No existe ningún proceso", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnrecognisedQuestion_ReturnsHelpListingAvailableTools()
    {
        var tool = AIFunctionFactory.Create(() => "[]", "buscar_personas");

        using var client = new MockChatClient();
        var response = await client.GetResponseAsync(Ask("hola"), new ChatOptions { Tools = [tool] });

        Assert.Contains("buscar_personas", response.Text, StringComparison.Ordinal);
        Assert.Empty(response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>());
    }

    [Fact]
    public async Task CaseNumberQuestion_WithoutTool_ExplainsMissingTool()
    {
        using var client = new MockChatClient();
        var response = await client.GetResponseAsync(Ask("Estado de EXP-2024-00031"));

        Assert.Contains("obtener_proceso", response.Text, StringComparison.Ordinal);
    }
}
