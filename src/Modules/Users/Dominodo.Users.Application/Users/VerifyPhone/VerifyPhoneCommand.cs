using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Users.VerifyPhone;

internal sealed record VerifyPhoneCommand(string Phone, string Code) : ICommand;
