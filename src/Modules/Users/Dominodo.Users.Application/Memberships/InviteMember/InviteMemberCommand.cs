using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Users.Application.Memberships.InviteMember;

internal sealed record InviteMemberCommand(string Phone, int RoleId) : ICommand<Guid>;
