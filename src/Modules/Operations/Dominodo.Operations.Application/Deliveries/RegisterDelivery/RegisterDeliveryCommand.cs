using Dominodo.Operations.Application.Abstractions;
using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Deliveries;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;
using FluentValidation;

namespace Dominodo.Operations.Application.Deliveries.RegisterDelivery;

// Registers a delivery (deliveries.create — vigilante). Validates the destination apartment exists and
// the Deliveries feature is enabled for the tenant (cross-module reads via the Tenants facade).
// Code = PAQ-{year}-{value:D4} (per tenant + year).
internal sealed record RegisterDeliveryCommand(
    Guid ApartmentId,
    DeliveryType Type,
    string? Carrier,
    string? Comment,
    string? PhotoUrl,
    string? Metadata) : ICommand<DeliveryDto>;

internal sealed class RegisterDeliveryCommandValidator : AbstractValidator<RegisterDeliveryCommand>
{
    public RegisterDeliveryCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Carrier).MaximumLength(100);
        RuleFor(x => x.Comment).MaximumLength(1000);
        RuleFor(x => x.PhotoUrl).MaximumLength(2000);
    }
}

internal sealed class RegisterDeliveryCommandHandler(
    IDeliveryRepository deliveries,
    ISequenceProvider sequences,
    ITenantContext tenant,
    ICurrentUser currentUser,
    ITenantsModuleApi tenantsModule,
    IClock clock)
    : ICommandHandler<RegisterDeliveryCommand, DeliveryDto>
{
    private const string DeliveriesFeature = "Deliveries";
    private const string CodePrefix = "PAQ";

    public async Task<Result<DeliveryDto>> Handle(RegisterDeliveryCommand command, CancellationToken ct)
    {
        if (!await tenantsModule.ApartmentExistsAsync(command.ApartmentId, tenant.TenantId, ct))
        {
            return Error.NotFound("Apartment.NotFound", "The destination apartment does not exist.");
        }

        if (!await tenantsModule.IsFeatureEnabledAsync(tenant.TenantId, DeliveriesFeature, ct))
        {
            return Error.Conflict("Delivery.FeatureDisabled", "The Deliveries feature is not enabled for this tenant.");
        }

        var now = clock.UtcNow;
        var value = await sequences.NextAsync(CodePrefix, now.Year, ct);
        var code = $"{CodePrefix}-{now.Year}-{value:D4}";

        var result = Delivery.Register(
            tenant.TenantId,
            code,
            command.ApartmentId,
            command.Type,
            currentUser.UserId,
            now,
            command.Carrier,
            command.Comment,
            command.PhotoUrl);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var delivery = result.Value;
        if (command.Metadata is not null)
        {
            delivery.SetMetadata(command.Metadata);
        }

        deliveries.Add(delivery);
        return DeliveryMappers.ToDto(delivery);
    }
}
