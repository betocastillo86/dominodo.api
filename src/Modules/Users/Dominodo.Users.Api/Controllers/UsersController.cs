using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Users.Application.Users.GetUserById;
using Dominodo.Users.Application.Users.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Users.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpPost]
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
