using Dominodo.Shared.Infrastructure.Http;
using Dominodo.Users.Application.Users.Login;
using Dominodo.Users.Application.Users.Logout;
using Dominodo.Users.Application.Users.RefreshToken;
using Dominodo.Users.Application.Users.RequestPhoneVerification;
using Dominodo.Users.Application.Users.VerifyPhone;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Users.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("verify/request")]
    public async Task<IResult> RequestVerification(RequestVerificationRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RequestPhoneVerificationCommand(request.Phone), ct);
        return result.IsSuccess ? Results.Accepted() : result.ToProblem();
    }

    [HttpPost("verify/confirm")]
    public async Task<IResult> ConfirmVerification(ConfirmVerificationRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new VerifyPhoneCommand(request.Phone, request.Code), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPost("login")]
    public async Task<IResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request.Phone, request.Password), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("refresh")]
    public async Task<IResult> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RefreshTokenCommand(request.Token), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("logout")]
    public async Task<IResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new LogoutCommand(request.Token), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}

public sealed record RequestVerificationRequest(string Phone);
public sealed record ConfirmVerificationRequest(string Phone, string Code);
public sealed record LoginRequest(string Phone, string Password);
public sealed record RefreshRequest(string Token);
