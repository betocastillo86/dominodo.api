using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Memberships.AcceptInvitation;

// Self-service: the caller (UserId from the sub claim) accepts their own invitation in the current tenant.
internal sealed record AcceptInvitationCommand(Guid UserId) : ICommand;
