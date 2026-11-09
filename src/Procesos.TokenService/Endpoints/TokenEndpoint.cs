using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Procesos.Security;

namespace Procesos.TokenService.Endpoints;

/// <summary>
/// Endpoint OAuth2 "client_credentials" (forma estándar: <c>POST /connect/token</c>,
/// <c>application/x-www-form-urlencoded</c>) para que servicios internos (hoy: Procesos.ChatApi)
/// obtengan un Bearer token de corta vida para invocar Procesos.McpServer.
///
/// Es intencionalmente un STS mínimo "hecho a mano" para fines de laboratorio: la forma del
/// protocolo es estándar para que, en un escenario real, el reemplazo por un IdP (Entra ID,
/// Duende IdentityServer, Keycloak) sea un cambio de configuración y no de código consumidor.
/// Ver docs/SECURITY.md.
/// </summary>
public static class TokenEndpoint
{
    public static RouteGroupBuilder MapTokenEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/connect");

        group.MapPost("/token", HandleAsync)
            .RequireRateLimiting(RateLimiting.TokenEndpointPolicy)
            .WithName("IssueServiceToken")
            .Produces<TokenSuccessResponse>(StatusCodes.Status200OK)
            .Produces<OAuthErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<OAuthErrorResponse>(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static IResult HandleAsync(
        HttpRequest request,
        IOptions<JwtOptions> jwtOptions,
        IOptions<RegisteredServiceClientsOptions> clientsOptions,
        ILogger<RegisteredServiceClient> logger)
    {
        if (!request.HasFormContentType)
        {
            return BadRequest("invalid_request", "Se esperaba application/x-www-form-urlencoded.");
        }

        var form = request.Form;
        var grantType = form["grant_type"].ToString();
        var clientId = form["client_id"].ToString();
        var clientSecret = form["client_secret"].ToString();
        var requestedScope = form["scope"].ToString();

        if (grantType != "client_credentials")
        {
            return BadRequest("unsupported_grant_type", "Solo se soporta 'client_credentials'.");
        }

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return BadRequest("invalid_request", "client_id y client_secret son obligatorios.");
        }

        var client = clientsOptions.Value.Clients.Find(c => c.ClientId == clientId);

        // Comparación en tiempo constante del secreto para no filtrar, vía temporización,
        // si el client_id existe o no (mitigación de ataques de enumeración/timing).
        var expectedSecret = client?.ClientSecret ?? string.Empty;
        var secretsMatch = client is not null && FixedTimeEquals(clientSecret, expectedSecret);

        if (!secretsMatch)
        {
            logger.LogWarning("Intento de autenticación de cliente de servicio fallido para client_id={ClientId}.", SanitizeForLog(clientId));
            return InvalidClient();
        }

        var scope = string.IsNullOrWhiteSpace(requestedScope) ? McpSecurityDefaults.McpInvokeScope : requestedScope;

        if (!client!.AllowedScopes.Contains(scope))
        {
            logger.LogWarning("Cliente {ClientId} solicitó un scope no autorizado: {Scope}.", client.ClientId, SanitizeForLog(scope));
            return BadRequest("invalid_scope", $"El cliente no está autorizado para el scope '{scope}'.");
        }

        // A partir de aquí solo se usan valores de la configuración (client_id y scope registrados),
        // nunca los textos enviados por el llamador.
        var grantedScope = client.AllowedScopes.First(s => s == scope);
        var issued = JwtAccessTokenFactory.Create(jwtOptions.Value, client.ClientId, grantedScope);

        logger.LogInformation("Access token emitido para client_id={ClientId}, scope={Scope}, expira en {ExpiresIn}s.",
            client.ClientId, grantedScope, issued.ExpiresInSeconds);

        return Results.Ok(new TokenSuccessResponse(issued.AccessToken, "Bearer", issued.ExpiresInSeconds, grantedScope));
    }

    /// <summary>
    /// Neutraliza valores enviados por el llamador antes de escribirlos en logs (log forging):
    /// elimina saltos de línea y limita la longitud.
    /// </summary>
    private static string SanitizeForLog(string value)
    {
        var sanitized = value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
        return sanitized.Length <= 64 ? sanitized : sanitized[..64] + "…";
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);

        // CryptographicOperations.FixedTimeEquals exige igual longitud; si difieren, igual
        // comparamos contra sí mismo para no revelar la diferencia de longitud por timing.
        return bytesA.Length == bytesB.Length && CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }

    private static IResult BadRequest(string error, string description) =>
        Results.Json(new OAuthErrorResponse(error, description), statusCode: StatusCodes.Status400BadRequest);

    private static IResult InvalidClient() =>
        Results.Json(new OAuthErrorResponse("invalid_client", "Credenciales de cliente inválidas."), statusCode: StatusCodes.Status401Unauthorized);
}

// Nombres de propiedad en snake_case vía JsonPropertyName para respetar la forma estándar
// de la respuesta OAuth2 (RFC 6749 §5.1) que cualquier cliente OAuth2 ya sabe interpretar.
public sealed record TokenSuccessResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("scope")] string Scope);

public sealed record OAuthErrorResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("error_description")] string ErrorDescription);
