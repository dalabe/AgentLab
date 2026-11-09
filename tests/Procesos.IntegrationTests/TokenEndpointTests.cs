using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Procesos.IntegrationTests;

/// <summary>Dynamic tests of <c>POST /connect/token</c> (ADR-0004, specs/http-api.md#tokenservice).</summary>
public sealed class TokenEndpointTests(TokenServiceFactory factory) : IClassFixture<TokenServiceFactory>
{
    private static FormUrlEncodedContent Form(params (string Key, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));

    private Task<HttpResponseMessage> PostTokenAsync(HttpContent content) =>
        factory.CreateClient().PostAsync(new Uri("/connect/token", UriKind.Relative), content);

    [Fact]
    public async Task ValidCredentials_ReturnBearerJwt()
    {
        var response = await PostTokenAsync(Form(
            ("grant_type", "client_credentials"),
            ("client_id", TestSecrets.ClientId),
            ("client_secret", TestSecrets.ClientSecret),
            ("scope", "mcp.tools.invoke")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Bearer", body.RootElement.GetProperty("token_type").GetString());
        Assert.Equal(300, body.RootElement.GetProperty("expires_in").GetInt32());
        Assert.Equal("mcp.tools.invoke", body.RootElement.GetProperty("scope").GetString());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.RootElement.GetProperty("access_token").GetString());
        Assert.Equal("procesos-token-service", jwt.Issuer);
        Assert.Contains("procesos-mcp-server", jwt.Audiences);
        Assert.Equal(TestSecrets.ClientId, jwt.Subject);
    }

    [Theory]
    [InlineData(TestSecrets.ClientId, "wrong-secret")]
    [InlineData("unknown-client", TestSecrets.ClientSecret)]
    public async Task InvalidCredentials_Return401_WithUniformBody(string clientId, string secret)
    {
        var response = await PostTokenAsync(Form(
            ("grant_type", "client_credentials"),
            ("client_id", clientId),
            ("client_secret", secret)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_client", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task UnsupportedGrantType_Returns400()
    {
        var response = await PostTokenAsync(Form(
            ("grant_type", "password"),
            ("client_id", TestSecrets.ClientId),
            ("client_secret", TestSecrets.ClientSecret)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("unsupported_grant_type", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task UnauthorizedScope_Returns400InvalidScope()
    {
        var response = await PostTokenAsync(Form(
            ("grant_type", "client_credentials"),
            ("client_id", TestSecrets.ClientId),
            ("client_secret", TestSecrets.ClientSecret),
            ("scope", "admin")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_scope", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task NonFormContent_Returns400()
    {
        var response = await PostTokenAsync(JsonContent.Create(new { grant_type = "client_credentials" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
