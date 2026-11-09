using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Procesos.McpServer.Tools;
using Procesos.TokenService.Endpoints;

namespace Procesos.IntegrationTests;

/// <summary>Test-only secrets. Never reuse outside tests.</summary>
internal static class TestSecrets
{
    public const string SigningKey = "integration-test-signing-key-at-least-32-bytes!!";
    public const string ClientId = "chatapi-client";
    public const string ClientSecret = "integration-test-client-secret";
}

/// <summary>
/// Hosts Procesos.TokenService in memory. Environment "Testing" avoids loading the developer's
/// User Secrets; settings are applied with UseSetting so they are visible to eager reads in Program.cs.
/// </summary>
public sealed class TokenServiceFactory : WebApplicationFactory<TokenSuccessResponse>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Security:Jwt:SigningKey", TestSecrets.SigningKey);
        builder.UseSetting("Security:RegisteredClients:Clients:0:ClientId", TestSecrets.ClientId);
        builder.UseSetting("Security:RegisteredClients:Clients:0:ClientSecret", TestSecrets.ClientSecret);
        builder.UseSetting("Security:RegisteredClients:Clients:0:AllowedScopes:0", "mcp.tools.invoke");
    }
}

/// <summary>
/// Hosts Procesos.McpServer in memory. No database connection is opened because the tests only
/// exercise the authentication/authorization pipeline, which runs before any tool.
/// </summary>
public sealed class McpServerFactory : WebApplicationFactory<ProcesoTools>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Security:Jwt:SigningKey", TestSecrets.SigningKey);
        builder.UseSetting("ConnectionStrings:ProcesosDb", "Server=(localdb)\\unused;Database=unused;Trusted_Connection=True");
        builder.UseSetting("DocumentStorage:RootPath", Path.Combine(Path.GetTempPath(), "procesos-it-docs"));
    }
}
