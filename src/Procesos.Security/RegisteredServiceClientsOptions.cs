namespace Procesos.Security;

/// <summary>Un cliente de servicio autorizado a pedir tokens en Procesos.TokenService.</summary>
public sealed class RegisteredServiceClient
{
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Secreto del cliente. Configurado vía User Secrets, nunca en el repo.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    public string[] AllowedScopes { get; set; } = [];
}

/// <summary>
/// Registro (muy simplificado, en memoria/config) de los clientes de servicio autorizados a usar
/// el flujo client_credentials. En producción esto se reemplaza por un IdP real (Entra ID, Duende
/// IdentityServer, Keycloak) con su propio almacén de clientes; aquí basta con configuración para
/// mantener el laboratorio autocontenido. Ver docs/SECURITY.md.
/// </summary>
public sealed class RegisteredServiceClientsOptions
{
    public const string SectionName = "Security:RegisteredClients";

    public List<RegisteredServiceClient> Clients { get; set; } = [];
}
