using Dominodo.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dominodo.Shared.Infrastructure.Http;

// Maps MVC model-binding / JSON deserialization failures (e.g. an unknown or out-of-range value for an
// enum property bound via JsonStringEnumConverter) to the SAME RFC 9457 ProblemDetails shape the
// ValidationBehavior produces for FluentValidation failures — title "Validation.Failed" plus an
// errors[] of {property, message} — so the error contract is uniform whether a request fails at the
// binding edge or inside a validator (docs 04 / 08). Wired via ConfigureApiBehaviorOptions.
public static class ValidationProblem
{
    public static IActionResult From(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors.Select(error => new ValidationError(
                NormalizeKey(entry.Key),
                string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The value is invalid." : error.ErrorMessage)))
            .ToList();

        var problem = new ProblemDetails
        {
            Title = "Validation.Failed",
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
        };
        problem.Extensions["errors"] = errors;

        return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
    }

    // System.Text.Json reports the JSON path as the ModelState key ("$.valueType", "$.items[0].type");
    // strip the "$." root and any parent path so the reported property matches the request field name.
    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }

        var name = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
        var lastDot = name.LastIndexOf('.');
        if (lastDot >= 0)
        {
            name = name[(lastDot + 1)..];
        }

        return name;
    }
}
