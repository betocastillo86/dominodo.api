using Dominodo.Shared.Infrastructure.Auth;
using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Shared.Kernel.Authorization;
using Dominodo.Shared.Kernel.Pagination;
using Dominodo.Users.Application.Users.GetUserById;
using Dominodo.Users.Application.Users.ListUsers;
using Dominodo.Users.Application.Users.RegisterUser;
using Dominodo.Users.Application.Users.UpdateUser;
using Dominodo.Users.Contracts;
using Dominodo.Users.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Users.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Registers a new user account.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Register(RegisterUserRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new RegisterUserCommand(
                request.Phone,
                request.Email,
                request.FirstName,
                request.LastName,
                request.Password),
            ct);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetUserById", new { id = result.Value }, new { id = result.Value })
            : result.ToProblem();
    }

    [HttpGet("{id:guid}", Name = "GetUserById")]
    [EndpointSummary("Gets a user by its identifier.")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpGet]
    [HasPermission(Permissions.UsersView)]
    [EndpointSummary("Lists users, paged and filterable (admin).")]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? name = null,
        [FromQuery] string? email = null,
        [FromQuery] string? phone = null,
        [FromQuery] UserStatus? status = null,
        [FromQuery] string? documentNumber = null,
        [FromQuery] bool? phoneVerified = null,
        [FromQuery] bool? emailVerified = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new ListUsersQuery(tenantId, name, email, phone, status, documentNumber, phoneVerified, emailVerified, page, pageSize),
            ct);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.UsersEdit)]
    [EndpointSummary("Updates a user's profile. Requires the users.edit permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateUserCommand(id, request.FirstName, request.LastName, request.Email, request.PreferredLanguage),
            ct);

        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record RegisterUserRequest(
    string Phone,
    string? Email,
    string FirstName,
    string LastName,
    string Password);

public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? Email,
    string PreferredLanguage);
