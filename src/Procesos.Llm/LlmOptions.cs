namespace Procesos.Llm;

/// <summary>Opciones del proveedor de LLM usado por Procesos.ChatApi para responder preguntas en lenguaje humano.</summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>"Anthropic" (por defecto) o "Mock" (sin API key, heurística local para probar sin costo/conexión).</summary>
    public string Provider { get; set; } = "Anthropic";

    public AnthropicLlmOptions Anthropic { get; set; } = new();
}

public sealed class AnthropicLlmOptions
{
    /// <summary>Configurado vía `dotnet user-secrets set "Llm:Anthropic:ApiKey" "<valor>"`. Nunca en el repo.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "claude-sonnet-5";

    public int MaxOutputTokens { get; set; } = 1500;
}
