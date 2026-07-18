using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;

namespace Dominodo.Tenants.Application.Tenants.GetTenantById;

internal sealed record GetTenantByIdQuery(Guid TenantId) : IQuery<TenantDetailDto>;
