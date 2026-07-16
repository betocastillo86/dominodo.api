using Dominodo.Shared.Kernel;
using Microsoft.AspNetCore.Http;

namespace Dominodo.Shared.Infrastructure.Http;

public static class ErrorResults
{
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Successful result has no problem.");
        }

        var error = result.Error;
        var status = error.Type switch
        {
            ErrorType.Validation   => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden    => StatusCodes.Status403Forbidden,
            ErrorType.NotFound     => StatusCodes.Status404NotFound,
            ErrorType.Conflict     => StatusCodes.Status409Conflict,
            _                      => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: status,
            extensions: BuildExtensions(result));
    }

    private static IDictionary<string, object?>? BuildExtensions(Result result) =>
        result is IValidationResult { Errors.Count: > 0 } v
            ? new Dictionary<string, object?> { ["errors"] = v.Errors }
            : null;
}
