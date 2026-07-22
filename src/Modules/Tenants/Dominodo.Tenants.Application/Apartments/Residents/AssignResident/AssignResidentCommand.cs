using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Domain.Apartments;
using Dominodo.Tenants.Domain.Ports;
using Dominodo.Users.Contracts;
using FluentValidation;

namespace Dominodo.Tenants.Application.Apartments.Residents.AssignResident;

internal sealed record AssignResidentCommand(
    Guid ApartmentId,
    Guid UserId,
    ResidentRelationType RelationType,
    bool LivesHere,
    DateOnly? StartDate) : ICommand<Guid>;

internal sealed class AssignResidentCommandValidator : AbstractValidator<AssignResidentCommand>
{
    public AssignResidentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RelationType).IsInEnum();
    }
}

// Loads the tenant-scoped Apartment aggregate, validates the referenced user exists in Users via the
// synchronous facade (cross-module read, doc 07 — NO FK across modules), and mutates through the aggregate.
internal sealed class AssignResidentCommandHandler(
    IApartmentRepository apartments,
    IUsersModuleApi usersModule)
    : ICommandHandler<AssignResidentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AssignResidentCommand command, CancellationToken ct)
    {
        var apartment = await apartments.GetByIdWithResidentsAsync(command.ApartmentId, ct);
        if (apartment is null)
        {
            return Error.NotFound("Apartment.NotFound", "Apartment not found.");
        }

        var user = await usersModule.GetUserByIdAsync(command.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User.NotFound", "The referenced user does not exist.");
        }

        var result = apartment.AssignResident(command.UserId, command.RelationType, command.LivesHere, command.StartDate);
        if (result.IsFailure)
        {
            return result.Error;
        }

        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return result.Value.Id;
    }
}
