using Dominodo.E2E.Clients.Core;
using Dominodo.E2E.Clients.Core.Models;
using Refit;
using Shouldly;

namespace Dominodo.E2E.Tests.Shared.Assertions;

/// <summary>
/// Fluent assertions over the API's RFC 9457 ProblemDetails responses — the Dominodo equivalent of
/// Pollaya's <c>ShouldHaveValidationError</c> helpers, adapted to our error shape
/// (<c>title = error code</c>; validation 400s carry <c>errors[] = {property, message}</c>).
/// </summary>
public static class ProblemDetailsAssertions
{
    public const string ValidationFailedTitle = "Validation.Failed";

    // ---- ProblemDetailsModel-level ----

    /// <summary>Asserts the problem is a validation failure carrying an error for <paramref name="property"/>.</summary>
    public static ProblemDetailsModel ShouldHaveValidationError(this ProblemDetailsModel? problem, string property)
    {
        problem.ShouldNotBeNull();
        problem!.Title.ShouldBe(ValidationFailedTitle);
        problem.Errors.ShouldNotBeNull();
        problem.Errors!.ShouldContain(
            e => string.Equals(e.Property, property, StringComparison.OrdinalIgnoreCase),
            $"Expected a validation error for '{property}'. Actual errors: {Describe(problem.Errors)}");
        return problem;
    }

    /// <summary>As above, and the error message for <paramref name="property"/> must contain <paramref name="containsMessage"/>.</summary>
    public static ProblemDetailsModel ShouldHaveValidationError(this ProblemDetailsModel? problem, string property, string containsMessage)
    {
        problem.ShouldHaveValidationError(property);
        problem!.Errors!.ShouldContain(
            e => string.Equals(e.Property, property, StringComparison.OrdinalIgnoreCase)
                 && e.Message != null
                 && e.Message.Contains(containsMessage),
            $"Expected a validation error for '{property}' containing '{containsMessage}'. Actual errors: {Describe(problem!.Errors!)}");
        return problem;
    }

    /// <summary>Asserts the problem's title equals the given error code (e.g. "User.NotFound").</summary>
    public static ProblemDetailsModel ShouldHaveErrorCode(this ProblemDetailsModel? problem, string code)
    {
        problem.ShouldNotBeNull();
        problem!.Title.ShouldBe(code);
        return problem;
    }

    // ---- ApiResponse<T>-level convenience (mirrors Pollaya's response-level helpers) ----

    public static ProblemDetailsModel ShouldHaveValidationError<T>(this ApiResponse<T> response, string property)
    {
        return response.GetProblem().ShouldHaveValidationError(property);
    }

    public static ProblemDetailsModel ShouldHaveValidationError<T>(this ApiResponse<T> response, string property, string containsMessage)
    {
        return response.GetProblem().ShouldHaveValidationError(property, containsMessage);
    }

    public static ProblemDetailsModel ShouldHaveErrorCode<T>(this ApiResponse<T> response, string code)
    {
        return response.GetProblem().ShouldHaveErrorCode(code);
    }

    private static string Describe(IEnumerable<ValidationErrorModel> errors)
    {
        return string.Join(", ", errors.Select(e => $"{e.Property}:{e.Message}"));
    }
}
