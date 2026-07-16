using Dominodo.Shared.Kernel;

namespace Dominodo.Shared.Infrastructure.Behaviors;

internal static class ValidationResultFactory
{
    public static Result Create<TResult>(IReadOnlyList<ValidationError> errors)
        where TResult : Result
    {
        if (typeof(TResult) == typeof(Result))
        {
            return new ValidationResult(errors);
        }

        var valueType = typeof(TResult).GetGenericArguments()[0];
        var resultType = typeof(ValidationResult<>).MakeGenericType(valueType);
        return (Result)Activator.CreateInstance(resultType, [errors])!;
    }
}

internal sealed class ValidationResult(IReadOnlyList<ValidationError> errors)
    : Result(false, Error.Validation("Validation.Failed", "One or more validation errors occurred.")),
      IValidationResult
{
    public IReadOnlyList<ValidationError> Errors { get; } = errors;
}

internal sealed class ValidationResult<TValue>(IReadOnlyList<ValidationError> errors)
    : Result<TValue>(default, false, Error.Validation("Validation.Failed", "One or more validation errors occurred.")),
      IValidationResult
{
    public IReadOnlyList<ValidationError> Errors { get; } = errors;
}
