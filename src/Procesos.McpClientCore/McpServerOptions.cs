namespace Procesos.McpClientCore;

/// <summary>Ubicación del MCP Server que este cliente debe consumir.</summary>
public sealed class McpServerOptions
{
    public const string SectionName = "Mcp:Server";

    /// <summary>URL base del endpoint MCP (Streamable HTTP), p.ej. "http://localhost:5071/".</summary>
    public string Endpoint { get; set; } = string.Empty;
}
