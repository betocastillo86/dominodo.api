using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Users.Logout;

internal sealed record LogoutCommand(string Token) : ICommand;
