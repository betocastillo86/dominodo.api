using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Dominodo.Shared.Infrastructure.Auth;

internal sealed class JwtTokenGenerator(IOptions<JwtOptions> options, IClock clock) : IJwtTokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateAccessToken(Guid userId, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = clock.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: clock.UtcNow.UtcDateTime,
            expires: expiry.UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string Plain, string Hash) GenerateRefreshToken()
    {
        var plain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(plain)));
        return (plain, hash);
    }
}
