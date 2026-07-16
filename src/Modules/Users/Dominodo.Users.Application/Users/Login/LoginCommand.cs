using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Users.Login;

internal sealed record LoginCommand(string Phone, string Password) : ICommand<LoginResponse>;

internal sealed record LoginResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
