namespace Dominodo.E2E.Clients.Core.Models;

public sealed class ProblemDetailsModel
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int Status { get; init; }
    public string? Detail { get; init; }
    public ValidationErrorModel[]? Errors { get; init; }
}

public sealed class ValidationErrorModel
{
    public string? Property { get; init; }
    public string? Message { get; init; }
}
