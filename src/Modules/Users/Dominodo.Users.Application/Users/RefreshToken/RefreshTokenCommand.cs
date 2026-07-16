using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Application.Users.Login;

namespace Dominodo.Users.Application.Users.RefreshToken;

internal sealed record RefreshTokenCommand(string Token) : ICommand<LoginResponse>;
