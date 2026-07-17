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
[Produces("application/json")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("verify/request")]
    [EndpointSummary("Requests an OTP code to be sent to the given phone number.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> RequestVerification(RequestVerificationRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RequestPhoneVerificationCommand(request.Phone), ct);
        return result.IsSuccess ? Results.Accepted() : result.ToProblem();
    }

    [HttpPost("verify/confirm")]
    [EndpointSummary("Confirms a phone number using the OTP code previously sent.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> ConfirmVerification(ConfirmVerificationRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new VerifyPhoneCommand(request.Phone, request.Code), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    [HttpPost("login")]
    [EndpointSummary("Authenticates a user and issues an access/refresh token pair.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request.Phone, request.Password), ct);
        return result.IsSuccess ? Results.Ok(ToTokens(result.Value)) : result.ToProblem();
    }

    [HttpPost("refresh")]
    [EndpointSummary("Exchanges a valid refresh token for a new access/refresh token pair.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IResult> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RefreshTokenCommand(request.Token), ct);
        return result.IsSuccess ? Results.Ok(ToTokens(result.Value)) : result.ToProblem();
    }

    [HttpPost("logout")]
    [EndpointSummary("Revokes a refresh token, ending the session.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new LogoutCommand(request.Token), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    private static AuthTokensResponse ToTokens(LoginResponse response) =>
        new(response.AccessToken, response.RefreshToken, response.ExpiresAt);
}

public sealed record RequestVerificationRequest(string Phone);
public sealed record ConfirmVerificationRequest(string Phone, string Code);
public sealed record LoginRequest(string Phone, string Password);
public sealed record RefreshRequest(string Token);
