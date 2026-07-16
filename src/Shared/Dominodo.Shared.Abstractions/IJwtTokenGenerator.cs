namespace Dominodo.Shared.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, IEnumerable<string> roles);
    (string Plain, string Hash) GenerateRefreshToken();
}
