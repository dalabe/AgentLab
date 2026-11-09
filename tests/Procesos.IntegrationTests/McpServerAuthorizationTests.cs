using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Procesos.Security;

namespace Procesos.IntegrationTests;

/// <summary>McpServer must reject unauthenticated (401) and under-scoped (403) callers — NFR-01, NFR-06.</summary>
public sealed class McpServerAuthorizationTests(McpServerFactory factory) : IClassFixture<McpServerFactory>
{
    private const string InitializeRequest =
        """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"tests","version":"1.0"}}}""";

    private static HttpRequestMessage McpRequest(string? bearer)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative))
        {
            Content = new StringContent(InitializeRequest, Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Headers.Add("Origin", "http://evil.example");
        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        return request;
    }

    private static string Token(string scope, string signingKey = TestSecrets.SigningKey) =>
        JwtAccessTokenFactory.Create(new JwtOptions { SigningKey = signingKey }, TestSecrets.ClientId, scope).AccessToken;

    [Fact]
    public async Task NoToken_Returns401()
    {
        var response = await factory.CreateClient().SendAsync(McpRequest(null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithWrongKey_Returns401()
    {
        var response = await factory.CreateClient().SendAsync(McpRequest(Token(McpSecurityDefaults.McpInvokeScope, "a-different-signing-key-of-sufficient-length!!")));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenWithoutRequiredScope_Returns403()
    {
        var response = await factory.CreateClient().SendAsync(McpRequest(Token("some.other.scope")));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_PassesAuthorization_AndInitializes()
    {
        var response = await factory.CreateClient().SendAsync(McpRequest(Token(McpSecurityDefaults.McpInvokeScope)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("serverInfo", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Responses_HaveNoCorsHeaders()
    {
        var response = await factory.CreateClient().SendAsync(McpRequest(Token(McpSecurityDefaults.McpInvokeScope)));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
