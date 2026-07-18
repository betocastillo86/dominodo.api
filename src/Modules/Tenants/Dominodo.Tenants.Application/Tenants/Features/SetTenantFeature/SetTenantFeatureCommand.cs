using Dominodo.Shared.Kernel.Messaging;

namespace Dominodo.Tenants.Application.Tenants.Features.SetTenantFeature;

internal sealed record SetTenantFeatureCommand(Guid TenantId, string FeatureKey, bool Enabled) : ICommand;
