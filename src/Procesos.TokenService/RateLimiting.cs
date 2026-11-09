namespace Procesos.TokenService;

internal static class RateLimiting
{
    /// <summary>
    /// Nombre de la policy de rate limiting aplicada a /connect/token para blindar el emisor de
    /// tokens frente a fuerza bruta / credential stuffing sobre client_secret.
    /// </summary>
    public const string TokenEndpointPolicy = "token-endpoint";
}
