using ModelContextProtocol.Client;

namespace Procesos.McpClientCore;

/// <summary>
/// Da acceso a un <see cref="McpClient"/> ya conectado al MCP Server de este laboratorio, con el
/// Bearer token de servicio adjunto automáticamente a cada request. El cliente MCP subyacente se
/// crea una sola vez (lazy, thread-safe) y se reutiliza para todas las llamadas de un proceso.
/// </summary>
public interface IProcesosMcpClientAccessor
{
    Task<McpClient> GetClientAsync(CancellationToken cancellationToken = default);
}
