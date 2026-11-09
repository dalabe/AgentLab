using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Procesos.Security;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra validación de JWT Bearer (issuer/audience/firma/vigencia) y una policy de
    /// autorización que exige el scope <see cref="McpSecurityDefaults.McpInvokeScope"/>.
    /// Se usa en Procesos.McpServer, que es el recurso protegido de este laboratorio.
    /// </summary>
    public static IServiceCollection AddProcesosResourceAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            throw new InvalidOperationException(
                $"Falta '{JwtOptions.SectionName}:SigningKey'. Configúrelo con 'dotnet user-secrets set' " +
                "(debe ser el mismo valor configurado en Procesos.TokenService). Ver docs/SECURITY.md.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(McpSecurityDefaults.McpInvokePolicy, policy =>
                policy.RequireClaim(McpSecurityDefaults.ScopeClaimType, McpSecurityDefaults.McpInvokeScope));

        return services;
    }

    /// <summary>
    /// Registra <see cref="IServiceTokenProvider"/> como typed HttpClient: el componente que
    /// usa Procesos.ChatApi (vía Procesos.McpClientCore) para obtener el Bearer token antes de
    /// llamar a Procesos.McpServer.
    /// </summary>
    public static IServiceCollection AddProcesosServiceTokenClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClientCredentialsOptions>(configuration.GetSection(ClientCredentialsOptions.SectionName));
        services.AddHttpClient<IServiceTokenProvider, ClientCredentialsTokenProvider>();
        return services;
    }
}
