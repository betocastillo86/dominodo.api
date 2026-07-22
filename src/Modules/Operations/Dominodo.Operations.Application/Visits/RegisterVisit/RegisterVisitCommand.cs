using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Visits;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;
using Dominodo.Users.Contracts;
using FluentValidation;

namespace Dominodo.Operations.Application.Visits.RegisterVisit;

// Registers a visit (visits.create — vigilante). Validates the destination apartment exists and the
// Visits feature is enabled (cross-module reads via the Tenants facade); an optional authorizing resident
// is validated against Users when supplied. No readable Code (domain-model §3.3).
internal sealed record RegisterVisitCommand(
    Guid ApartmentId,
    VisitType Type,
    string VisitorName,
    string? VisitorDocument,
    string? PhotoUrl,
    string? VehiclePlate,
    Guid? AuthorizedByUserId,
    string? Metadata) : ICommand<VisitDto>;

internal sealed class RegisterVisitCommandValidator : AbstractValidator<RegisterVisitCommand>
{
    public RegisterVisitCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.VisitorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.VisitorDocument).MaximumLength(50);
        RuleFor(x => x.PhotoUrl).MaximumLength(2000);
        RuleFor(x => x.VehiclePlate).MaximumLength(20);
    }
}

internal sealed class RegisterVisitCommandHandler(
    IVisitRepository visits,
    ITenantContext tenant,
    ICurrentUser currentUser,
    IUsersModuleApi usersModule,
    ITenantsModuleApi tenantsModule,
    IClock clock)
    : ICommandHandler<RegisterVisitCommand, VisitDto>
{
    private const string VisitsFeature = "Visits";

    public async Task<Result<VisitDto>> Handle(RegisterVisitCommand command, CancellationToken ct)
    {
        if (!await tenantsModule.ApartmentExistsAsync(command.ApartmentId, tenant.TenantId, ct))
        {
            return Error.NotFound("Apartment.NotFound", "The destination apartment does not exist.");
        }

        if (!await tenantsModule.IsFeatureEnabledAsync(tenant.TenantId, VisitsFeature, ct))
        {
            return Error.Conflict("Visit.FeatureDisabled", "The Visits feature is not enabled for this tenant.");
        }

        if (command.AuthorizedByUserId is { } authorizerId)
        {
            var user = await usersModule.GetUserByIdAsync(authorizerId, ct);
            if (user is null)
            {
                return Error.NotFound("User.NotFound", "The authorizing user does not exist.");
            }
        }

        var result = Visit.Register(
            tenant.TenantId,
            command.ApartmentId,
            command.Type,
            command.VisitorName,
            currentUser.UserId,
            clock.UtcNow,
            command.VisitorDocument,
            command.PhotoUrl,
            command.VehiclePlate,
            command.AuthorizedByUserId);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var visit = result.Value;
        if (command.Metadata is not null)
        {
            visit.SetMetadata(command.Metadata);
        }

        visits.Add(visit);
        return VisitMappers.ToDto(visit);
    }
}
