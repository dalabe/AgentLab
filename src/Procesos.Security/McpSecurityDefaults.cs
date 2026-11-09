namespace Procesos.Security;

/// <summary>
/// Constantes compartidas del esquema de seguridad servicio-a-servicio (OAuth2 Client
/// Credentials + JWT de corta vida) usado entre Procesos.TokenService (emisor),
/// Procesos.McpServer (recurso protegido) y Procesos.ChatApi (cliente de servicio).
/// Ver docs/SECURITY.md para la justificación de este diseño.
/// </summary>
public static class McpSecurityDefaults
{
    /// <summary>Audiencia del token: identifica al MCP Server como recurso protegido.</summary>
    public const string Audience = "procesos-mcp-server";

    /// <summary>Emisor por defecto de los tokens (Procesos.TokenService).</summary>
    public const string Issuer = "procesos-token-service";

    /// <summary>Único scope del laboratorio: permiso para invocar tools del MCP Server.</summary>
    public const string McpInvokeScope = "mcp.tools.invoke";

    /// <summary>Tipo de claim bajo el cual se publica el scope en el JWT.</summary>
    public const string ScopeClaimType = "scope";

    /// <summary>Nombre de la policy de autorización que exige <see cref="McpInvokeScope"/>.</summary>
    public const string McpInvokePolicy = "McpToolsInvoke";
}
