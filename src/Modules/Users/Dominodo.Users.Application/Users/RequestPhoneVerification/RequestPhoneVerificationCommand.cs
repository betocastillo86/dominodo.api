using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Users.RequestPhoneVerification;

internal sealed record RequestPhoneVerificationCommand(string Phone) : ICommand;
