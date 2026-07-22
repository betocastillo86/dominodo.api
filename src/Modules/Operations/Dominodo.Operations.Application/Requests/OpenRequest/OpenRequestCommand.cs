using Dominodo.Operations.Application.Abstractions;
using Dominodo.Operations.Contracts;
using Dominodo.Operations.Domain.Ports;
using Dominodo.Operations.Domain.Requests;
using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Tenants.Contracts;
using Dominodo.Users.Contracts;
using FluentValidation;

namespace Dominodo.Operations.Application.Requests.OpenRequest;

// Opening a PQRS is a member action, NOT permission-gated (plan §Design decision). The controller only
// requires [Authorize]; this handler enforces: a resolved tenant + an ACTIVE membership in it. The
// reporter (current user) becomes the first participant. Code = SOL-{year}-{value:D4} (per tenant + year).
internal sealed record OpenRequestCommand(
    RequestType Type,
    string Title,
    string Description,
    RequestPriority Priority,
    Guid? ApartmentId,
    string? Category,
    string? Location,
    string? Metadata) : ICommand<RequestDto>;

internal sealed class OpenRequestCommandValidator : AbstractValidator<OpenRequestCommand>
{
    public OpenRequestCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(200);
    }
}

internal sealed class OpenRequestCommandHandler(
    IRequestRepository requests,
    ISequenceProvider sequences,
    ITenantContext tenant,
    ICurrentUser currentUser,
    IUsersModuleApi usersModule,
    ITenantsModuleApi tenantsModule,
    IClock clock)
    : ICommandHandler<OpenRequestCommand, RequestDto>
{
    private const string RequestsFeature = "Requests";
    private const string CodePrefix = "SOL";

    public async Task<Result<RequestDto>> Handle(OpenRequestCommand command, CancellationToken ct)
    {
        if (!tenant.HasTenant)
        {
            return Error.Forbidden("Request.TenantRequired", "A tenant (X-Tenant) is required to open a request.");
        }

        // Membership gate: the caller must be an ACTIVE member of the resolved tenant.
        var memberships = await usersModule.GetMembershipsAsync(currentUser.UserId, ct);
        var isActiveMember = memberships.Any(m =>
            m.TenantId == tenant.TenantId &&
            string.Equals(m.Status, "Active", StringComparison.OrdinalIgnoreCase));
        if (!isActiveMember)
        {
            return Error.Forbidden("Request.NotAMember", "You are not an active member of this tenant.");
        }

        if (!await tenantsModule.IsFeatureEnabledAsync(tenant.TenantId, RequestsFeature, ct))
        {
            return Error.Conflict("Request.FeatureDisabled", "The Requests feature is not enabled for this tenant.");
        }

        if (command.ApartmentId is { } apartmentId &&
            !await tenantsModule.ApartmentExistsAsync(apartmentId, tenant.TenantId, ct))
        {
            return Error.NotFound("Apartment.NotFound", "The referenced apartment does not exist.");
        }

        var now = clock.UtcNow;
        var value = await sequences.NextAsync(CodePrefix, now.Year, ct);
        var code = $"{CodePrefix}-{now.Year}-{value:D4}";

        var result = Request.Open(
            tenant.TenantId,
            code,
            command.Type,
            command.Title,
            command.Description,
            command.Priority,
            currentUser.UserId,
            now,
            command.ApartmentId,
            command.Category,
            command.Location);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var request = result.Value;
        if (command.Metadata is not null)
        {
            request.SetMetadata(command.Metadata);
        }

        requests.Add(request);
        // No SaveChangesAsync — the UnitOfWorkBehavior owns the transaction (doc 03).
        return RequestMappers.ToDto(request);
    }
}
