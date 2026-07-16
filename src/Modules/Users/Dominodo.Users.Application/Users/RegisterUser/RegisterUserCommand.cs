using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Users.RegisterUser;

internal sealed record RegisterUserCommand(
    string Phone,
    string? Email,
    string FirstName,
    string LastName,
    string Password) : ICommand<Guid>;
