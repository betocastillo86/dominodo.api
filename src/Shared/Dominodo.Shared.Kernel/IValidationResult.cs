namespace Dominodo.Shared.Kernel;

public sealed record ValidationError(string Property, string Message);

public interface IValidationResult
{
    IReadOnlyList<ValidationError> Errors { get; }
}
