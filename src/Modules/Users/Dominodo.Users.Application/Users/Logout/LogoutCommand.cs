using System.Security.Cryptography;
using System.Text;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Users.Logout;

internal sealed record LogoutCommand(string Token) : ICommand;

internal sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IClock clock)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken ct)
    {
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(command.Token)));
        var token = await refreshTokens.GetByHashAsync(hash, ct);

        if (token is not null && token.IsActive(clock))
        {
            token.Revoke(clock);
        }

        return Result.Success();
    }
}
