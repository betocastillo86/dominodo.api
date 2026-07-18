using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Dominodo.E2E.Core.Security;

/// <summary>
/// Mints signed JWTs on demand for authenticated E2E calls — a temporary stand-in for the
/// deferred real-login provider. Replicates <c>JwtTokenGenerator</c> exactly (HS256, UTF-8
/// signing key, <c>sub</c> + <c>jti</c> + one <see cref="ClaimTypes.Role"/> per role) so the
/// running API accepts the tokens. The <c>Jwt</c> settings must match the running environment.
/// </summary>
public sealed class JwtTokenFactory(JwtSettings settings)
{
    private readonly JwtSettings _settings = settings;

    /// <summary>Mints a token for the given user id and roles.</summary>
    public string CreateUserToken(Guid userId, params string[] roles)
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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Mints a token for the seeded user that carries <paramref name="permission"/>.
    /// The server resolves the permission set from the DB by the token sub, so the request
    /// is authorized if and only if the endpoint requires exactly that permission.
    /// </summary>
    public string GenerateToken(string permission)
    {
        var userId = DominodoConstants.IntegrationSeed.UserIdFor(permission);
        return CreateUserToken(userId);
    }

    /// <summary>
    /// Mints a token for the seeded "Rol Public" user — a real user assigned to a Platform role that
    /// carries zero permissions. Use to assert a <c>[HasPermission(code)]</c> endpoint returns 403 for a
    /// user that exists but lacks the permission (distinct from a token for an unknown user id).
    /// </summary>
    public string GeneratePublicToken() =>
        CreateUserToken(DominodoConstants.IntegrationSeed.PublicUserId);
}
