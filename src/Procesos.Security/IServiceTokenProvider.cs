namespace Procesos.Security;

/// <summary>
/// Obtiene (y cachea/renueva) un access token de servicio para llamar a un recurso protegido
/// (Procesos.McpServer) usando el flujo OAuth2 Client Credentials.
/// </summary>
public interface IServiceTokenProvider
{
    /// <summary>Devuelve un access token válido, renovándolo automáticamente si está vencido o próximo a vencer.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
