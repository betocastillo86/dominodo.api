using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Tenants.Application.Tenants.CreateTenant;

internal sealed record CreateTenantCommand(
    string Slug,
    string Name,
    string Type,
    string Address,
    string City,
    string Country,
    string? LegalId,
    string? Branding,
    string? Settings) : ICommand<Guid>;
