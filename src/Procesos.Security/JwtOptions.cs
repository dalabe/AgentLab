namespace Procesos.Security;

/// <summary>
/// Opciones del esquema JWT compartido entre emisor (TokenService) y validador (McpServer).
/// <see cref="SigningKey"/> es un secreto simétrico (HMAC-SHA256) que NUNCA debe vivir en
/// appsettings.json versionado: se configura vía `dotnet user-secrets` en desarrollo, o una
/// variable de entorno / Key Vault en un despliegue real. Ver docs/SECURITY.md.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Security:Jwt";

    /// <summary>Secreto simétrico usado para firmar y validar los JWT. Mínimo 32 bytes recomendable.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = McpSecurityDefaults.Issuer;

    public string Audience { get; set; } = McpSecurityDefaults.Audience;

    /// <summary>Vida útil del access token. Corta a propósito (mejor práctica de minimizar ventana de abuso).</summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 5;
}
