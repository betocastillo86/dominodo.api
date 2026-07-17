namespace Dominodo.Users.Api.Controllers;

// Access + refresh token pair issued by the authentication endpoints.
public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
