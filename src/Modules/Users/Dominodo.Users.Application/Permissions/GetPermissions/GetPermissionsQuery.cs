using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using Dominodo.Users.Contracts;
using Dominodo.Users.Domain.Ports;

namespace Dominodo.Users.Application.Permissions.GetPermissions;

internal sealed record GetPermissionsQuery : IQuery<IReadOnlyList<PermissionDto>>;

internal sealed class GetPermissionsQueryHandler(IPermissionRepository permissions)
    : IQueryHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    public async Task<Result<IReadOnlyList<PermissionDto>>> Handle(GetPermissionsQuery query, CancellationToken ct)
    {
        var all = await permissions.GetAllAsync(ct);
        IReadOnlyList<PermissionDto> dtos = all
            .Select(p => new PermissionDto(p.Id, p.Code, p.Description, p.Group))
            .ToList();
        return Result.Success(dtos);
    }
}
