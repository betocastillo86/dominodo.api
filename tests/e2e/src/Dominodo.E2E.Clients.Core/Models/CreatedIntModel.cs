namespace Dominodo.E2E.Clients.Core.Models;

/// <summary>
/// Hand-replicated response body for endpoints that return a created integer identifier,
/// e.g. <c>POST /api/v1/roles</c> → <c>{"id": 5}</c>.
/// </summary>
public sealed class CreatedIntModel
{
    public int Id { get; init; }
}
