using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Procesos.Security;

/// <summary>
/// Implementación de <see cref="IServiceTokenProvider"/> que habla el flujo estándar OAuth2
/// "client_credentials" contra Procesos.TokenService. Cachea el token en memoria del proceso y
/// lo renueva automáticamente ~30s antes de que expire, evitando una llamada de red por cada
/// invocación al MCP Server. Es thread-safe (un único refresh concurrente vía SemaphoreSlim).
/// </summary>
public sealed class ClientCredentialsTokenProvider : IServiceTokenProvider, IDisposable
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);

    private readonly HttpClient _httpClient;
    private readonly ClientCredentialsOptions _options;
    private readonly ILogger<ClientCredentialsTokenProvider> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAtUtc = DateTimeOffset.MinValue;

    public ClientCredentialsTokenProvider(
        HttpClient httpClient,
        IOptions<ClientCredentialsOptions> options,
        ILogger<ClientCredentialsTokenProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (TryGetCachedToken(out var cached))
        {
            return cached;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetCachedToken(out cached))
            {
                return cached;
            }

            return await RequestNewTokenAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool TryGetCachedToken(out string token)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAtUtc - RefreshSkew)
        {
            token = _cachedToken;
            return true;
        }

        token = string.Empty;
        return false;
    }

    private async Task<string> RequestNewTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.TokenEndpoint))
        {
            throw new InvalidOperationException(
                $"Security:ServiceClient:TokenEndpoint no está configurado para {nameof(ClientCredentialsTokenProvider)}.");
        }

        _logger.LogInformation("Solicitando nuevo access token de servicio a {Endpoint} (client_id={ClientId}).",
            _options.TokenEndpoint, _options.ClientId);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["scope"] = _options.Scope,
            }),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Procesos.TokenService rechazó la solicitud de token ({(int)response.StatusCode}): {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Procesos.TokenService devolvió una respuesta vacía o inválida.");

        _cachedToken = payload.AccessToken;
        _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn);

        _logger.LogInformation("Access token de servicio obtenido, expira en {ExpiresIn}s.", payload.ExpiresIn);

        return _cachedToken;
    }

    public void Dispose() => _refreshLock.Dispose();

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string TokenType);
}
