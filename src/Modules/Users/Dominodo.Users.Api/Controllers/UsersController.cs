using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Users.Application.Users.GetUserById;
using Dominodo.Users.Application.Users.RegisterUser;
using Dominodo.Users.Contracts;
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
}

public sealed record RegisterUserRequest(
    string Phone,
    string? Email,
    string FirstName,
    string LastName,
    string Password);
