using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Tenants.Application.Tenants.ChangeTenantStatus;

internal sealed record ChangeTenantStatusCommand(Guid TenantId, string Status) : ICommand;
