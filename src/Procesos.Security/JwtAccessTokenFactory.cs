using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Procesos.Security;

/// <summary>Resultado de emitir un access token: el JWT y su vigencia en segundos.</summary>
public sealed record IssuedAccessToken(string AccessToken, int ExpiresInSeconds);

/// <summary>
/// Construye los JWT de acceso de corta vida que Procesos.TokenService emite para el flujo
/// client_credentials. Centralizado aquí para que la forma de los claims (iss/aud/sub/scope/jti)
/// sea exactamente la que Procesos.McpServer espera validar.
/// </summary>
public static class JwtAccessTokenFactory
{
    public static IssuedAccessToken Create(JwtOptions options, string clientId, string scope)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(options.AccessTokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim(McpSecurityDefaults.ScopeClaimType, scope),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresInSeconds = (int)(expires - now).TotalSeconds;

        return new IssuedAccessToken(encoded, expiresInSeconds);
    }

    private static long ToUnixTimeSeconds(this DateTime dateTime) => new DateTimeOffset(dateTime).ToUnixTimeSeconds();
}
