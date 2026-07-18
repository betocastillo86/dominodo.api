namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/auth/verify/confirm</c>.
/// Mirrors the API's <c>ConfirmVerificationRequest</c> by value.
/// </summary>
public sealed record ConfirmOtpModel(string Phone, string Code);
