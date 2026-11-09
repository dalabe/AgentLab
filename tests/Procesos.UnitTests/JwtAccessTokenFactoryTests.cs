using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Procesos.Security;

namespace Procesos.UnitTests;

/// <summary>Token shape contract between TokenService (issuer) and McpServer (validator) — ADR-0004/0005.</summary>
public sealed class JwtAccessTokenFactoryTests
{
    private static readonly JwtOptions Options = new()
    {
        SigningKey = "unit-test-signing-key-that-is-at-least-32-bytes-long!",
        AccessTokenLifetimeMinutes = 5,
    };

    private static TokenValidationParameters ValidationParameters(string signingKey) => new()
    {
        ValidIssuer = McpSecurityDefaults.Issuer,
        ValidAudience = McpSecurityDefaults.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
    };

    [Fact]
    public void Create_ProducesTokenThatValidatesWithServerParameters()
    {
        var issued = JwtAccessTokenFactory.Create(Options, "chatapi-client", McpSecurityDefaults.McpInvokeScope);

        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }
            .ValidateToken(issued.AccessToken, ValidationParameters(Options.SigningKey), out var token);

        Assert.Equal("chatapi-client", principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal(McpSecurityDefaults.McpInvokeScope, principal.FindFirst(McpSecurityDefaults.ScopeClaimType)?.Value);
        Assert.False(string.IsNullOrEmpty(principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value));
        Assert.Equal(SecurityAlgorithms.HmacSha256, ((JwtSecurityToken)token).Header.Alg);
    }

    [Fact]
    public void Create_SetsShortLifetime()
    {
        var issued = JwtAccessTokenFactory.Create(Options, "chatapi-client", McpSecurityDefaults.McpInvokeScope);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.AccessToken);

        Assert.Equal(300, issued.ExpiresInSeconds);
        Assert.InRange(jwt.ValidTo - jwt.ValidFrom, TimeSpan.FromMinutes(4.9), TimeSpan.FromMinutes(5.1));
    }

    [Fact]
    public void Create_TokenIsRejected_WithDifferentSigningKey()
    {
        var issued = JwtAccessTokenFactory.Create(Options, "chatapi-client", McpSecurityDefaults.McpInvokeScope);

        Assert.ThrowsAny<SecurityTokenException>(() => new JwtSecurityTokenHandler()
            .ValidateToken(issued.AccessToken, ValidationParameters("another-key-another-key-another-key-123"), out _));
    }

    [Fact]
    public void Create_GeneratesUniqueJtiPerToken()
    {
        var handler = new JwtSecurityTokenHandler();
        var a = handler.ReadJwtToken(JwtAccessTokenFactory.Create(Options, "c", "s").AccessToken).Id;
        var b = handler.ReadJwtToken(JwtAccessTokenFactory.Create(Options, "c", "s").AccessToken).Id;

        Assert.NotEqual(a, b);
    }
}
