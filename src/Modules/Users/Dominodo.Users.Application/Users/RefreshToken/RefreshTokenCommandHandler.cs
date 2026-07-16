using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Application.Users.Login;
using Dominodo.Users.Domain.Authentication;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Users;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Dominodo.Users.Application.Users.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPlatformRoleAssignmentRepository platformRoleAssignments,
    IJwtTokenGenerator jwtGenerator,
    IClock clock,
    IOptions<JwtOptions> jwtOptions)
    : ICommandHandler<RefreshTokenCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var hash = ComputeHash(command.Token);
        var existing = await refreshTokens.GetByHashAsync(hash, ct);

        if (existing is null || !existing.IsActive(clock))
        {
            return Error.Unauthorized("Auth.InvalidRefreshToken", "The refresh token is invalid or expired.");
        }

        var user = await users.GetByIdAsync(existing.UserId, ct);
        if (user is null || user.Status != UserStatus.Active)
        {
            return Error.Unauthorized("Auth.InvalidRefreshToken", "The refresh token is invalid or expired.");
        }

        var roleNames = await platformRoleAssignments.GetPlatformRoleNamesForUserAsync(user.Id, ct);
        var accessToken = jwtGenerator.GenerateAccessToken(user.Id, roleNames);

        var (plain, newHash) = jwtGenerator.GenerateRefreshToken();
        var expiry = clock.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays);
        var newToken = Domain.Authentication.RefreshToken.Issue(user.Id, newHash, expiry);

        existing.Revoke(clock, replacedByTokenId: newToken.Id);
        refreshTokens.Add(newToken);

        return new LoginResponse(accessToken, plain, clock.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenExpirationMinutes));
    }

    private static string ComputeHash(string plain) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(plain)));
}
