using System.Net.Http.Headers;
using Procesos.Security;

namespace Procesos.McpClientCore;

/// <summary>
/// DelegatingHandler que adjunta el Bearer token de servicio (obtenido vía
/// <see cref="IServiceTokenProvider"/>, flujo OAuth2 client_credentials) a cada request HTTP
/// hecha al MCP Server. Mantiene la autenticación completamente fuera del protocolo MCP en sí:
/// el transporte HTTP solo necesita un HttpClient configurado con este handler.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IServiceTokenProvider _tokenProvider;

    public BearerTokenHandler(IServiceTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
