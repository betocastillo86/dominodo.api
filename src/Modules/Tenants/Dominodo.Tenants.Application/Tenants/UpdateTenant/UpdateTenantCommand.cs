using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Tenants.Application.Tenants.UpdateTenant;

// Slug is immutable once set (domain-model §2.1, plan Phase 2) and is therefore not part of the update.
internal sealed record UpdateTenantCommand(
    Guid TenantId,
    string Name,
    string? LegalId,
    string Address,
    string City,
    string Country) : ICommand;
