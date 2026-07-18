using Dominodo.Shared.Abstractions;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Application.Abstractions;
using DomainRefreshToken = Dominodo.Users.Domain.Authentication.RefreshToken;
using Dominodo.Users.Domain.Ports;
using Dominodo.Users.Domain.Users;
using Microsoft.Extensions.Options;

namespace Dominodo.Users.Application.Users.Login;

internal sealed class LoginCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPlatformRoleAssignmentRepository platformRoleAssignments,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtGenerator,
    IClock clock,
    IOptions<JwtOptions> jwtOptions)
    : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly int _refreshExpirationDays = jwtOptions.Value.RefreshTokenExpirationDays;

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await users.GetByPhoneAsync(command.Phone, ct);

        if (user is null)
        {
            return Error.Unauthorized("Auth.InvalidCredentials", "Phone or password is incorrect.");
        }

        if (user.Status != UserStatus.Active)
        {
            return Error.Forbidden("Auth.AccountNotActive", "Account is not active.");
        }

        if (user.PasswordHash is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Error.Unauthorized("Auth.InvalidCredentials", "Phone or password is incorrect.");
        }

        var roleNames = await platformRoleAssignments.GetPlatformRoleNamesForUserAsync(user.Id, ct);
        var accessToken = jwtGenerator.GenerateAccessToken(user.Id, roleNames);

        var (plain, hash) = jwtGenerator.GenerateRefreshToken();
        var expiry = clock.UtcNow.AddDays(_refreshExpirationDays);
        refreshTokens.Add(DomainRefreshToken.Issue(user.Id, hash, expiry));

        return new LoginResponse(accessToken, plain, clock.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenExpirationMinutes));
    }
}
