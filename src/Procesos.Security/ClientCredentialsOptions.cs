namespace Procesos.Security;

/// <summary>
/// Credenciales de cliente de servicio (OAuth2 Client Credentials) que usa Procesos.ChatApi
/// para autenticarse ante Procesos.TokenService y poder llamar a Procesos.McpServer.
/// <see cref="ClientSecret"/> se configura vía User Secrets/variables de entorno, nunca en el repo.
/// </summary>
public sealed class ClientCredentialsOptions
{
    public const string SectionName = "Security:ServiceClient";

    /// <summary>URL completa del endpoint de token, p.ej. "https://localhost:7101/connect/token".</summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string Scope { get; set; } = McpSecurityDefaults.McpInvokeScope;
}
