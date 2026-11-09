using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Procesos.Security;

namespace Procesos.McpClientCore;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra el cliente MCP de servicio: el <see cref="IServiceTokenProvider"/> (OAuth2
    /// client_credentials contra Procesos.TokenService) y el <see cref="IProcesosMcpClientAccessor"/>
    /// (cliente MCP sobre HTTP, con el Bearer token adjunto automáticamente) que usa Procesos.ChatApi
    /// para descubrir e invocar las tools expuestas por Procesos.McpServer.
    /// </summary>
    public static IServiceCollection AddProcesosMcpClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProcesosServiceTokenClient(configuration);

        services.Configure<McpServerOptions>(configuration.GetSection(McpServerOptions.SectionName));

        services.AddTransient<BearerTokenHandler>();
        services.AddHttpClient(ProcesosMcpClientAccessor.HttpClientName)
            .AddHttpMessageHandler<BearerTokenHandler>();

        services.AddSingleton<IProcesosMcpClientAccessor, ProcesosMcpClientAccessor>();

        return services;
    }
}
