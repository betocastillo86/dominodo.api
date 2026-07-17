namespace Dominodo.E2E.Core.Security;

/// <summary>
/// The JWT triple the suite uses to mint tokens. Must match the running API's
/// <c>Jwt</c> section (Issuer/Audience/SecretKey) so minted tokens pass validation.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
    public string SecretKey { get; init; } = default!;
    public int AccessTokenExpirationMinutes { get; init; } = 60;
}
