namespace Dominodo.E2E.Clients.Modules.Users.Models;

/// <summary>
/// Hand-replicated request body for <c>POST /api/v1/auth/verify/request</c>.
/// Mirrors the API's <c>RequestVerificationRequest</c> by value.
/// </summary>
public sealed record RequestOtpModel(string Phone);
