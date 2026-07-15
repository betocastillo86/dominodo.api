# 08 — Error Handling

## What it is

Failures travel through the application as `Result`/`Error` values
(see [02 — DDD Building Blocks](./02-ddd-building-blocks.md)). At the API edge they are translated
into HTTP responses using **RFC 9457 `ProblemDetails`** with the **correct status code for the kind
of failure**. Unexpected exceptions are caught by a global handler and returned as `500` — never as a
stack trace.

The failure's `Error.Type` — not the layer that produced it — decides the status code.

## Why

- Clients depend on status codes to behave correctly (retry, redirect to login, show a field error,
  surface a conflict). Collapsing everything into one status throws that information away.
- `ProblemDetails` is a standard, machine-readable envelope, so every client parses errors the same
  way.
- Mapping in one place keeps controllers thin and the taxonomy consistent across all modules.

> **Don't collapse every failure into `422`.** A single catch-all status forces clients to parse
> error strings to tell "you're not logged in" from "that field is too long" from "this record
> doesn't exist." Map each failure to the status that describes it.

## The taxonomy

| Situation | `ErrorType` | HTTP status |
| --- | --- | --- |
| Request is malformed / fails input validation | `Validation` | `400 Bad Request` |
| Caller is not authenticated | `Unauthorized` | `401 Unauthorized` |
| Caller is authenticated but not allowed | `Forbidden` | `403 Forbidden` |
| Referenced resource does not exist | `NotFound` | `404 Not Found` |
| Operation conflicts with current state | `Conflict` | `409 Conflict` |
| Well-formed request, allowed, but violates a business rule | (business rule) | `422 Unprocessable Entity` |
| Unexpected / unhandled | `Failure` / exception | `500 Internal Server Error` |

`422` is reserved for its precise meaning: the request was syntactically fine and authorized, but a
**semantic business rule** rejects it. It is not the default.

## Mapping Error → status

A single mapper converts an `Error` (and the optional validation details) into a `ProblemDetails`.

```csharp
// Dominodo.Shared.Infrastructure/Http/ErrorResults.cs
public static class ErrorResults
{
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess) throw new InvalidOperationException("Successful result has no problem.");

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
            extensions: ValidationDetails(result));
    }

    private static IDictionary<string, object?>? ValidationDetails(Result result) =>
        result is IValidationResult { Errors.Count: > 0 } v
            ? new Dictionary<string, object?> { ["errors"] = v.Errors }
            : null;
}
```

For validation failures the `errors` extension carries the per-field messages produced by the
`ValidationBehavior` (see [04 — Validation](./04-validation.md)), so a `400` looks like:

```json
{
  "type": "about:blank",
  "title": "Validation.Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": [
    { "property": "Subject", "message": "'Subject' must not be empty." }
  ]
}
```

## Controllers stay thin

Controllers are inbound adapters. They send the request and map the `Result` — no business logic, no
try/catch, no status-code decisions of their own.

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/pqrs")]
public sealed class PqrsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IResult> Open(OpenPqrRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new OpenPqrCommand(request.ApartmentId, request.Subject, request.Body), ct);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetPqrById", new { id = result.Value }, new { id = result.Value })
            : result.ToProblem();
    }

    [HttpGet("{id:guid}", Name = "GetPqrById")]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetPqrByIdQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }
}
```

Map success to the right status too: `201 Created` (with `Location`) for creation, `200 OK` for
reads, `204 No Content` for updates/deletes with no body.

## Unexpected exceptions — global handler

Anything not modeled as a `Result` (a bug, an infrastructure outage) is caught by an
`IExceptionHandler` that logs the full detail server-side and returns a safe `500` `ProblemDetails`
with **no internal information** leaked.

```csharp
// Dominodo.Shared.Infrastructure/Http/GlobalExceptionHandler.cs
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken token)
    {
        logger.LogError(ex, "Unhandled exception for {Path}", ctx.Request.Path);

        var problem = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "The request could not be completed. Please try again later."
        };
        ctx.Response.StatusCode = problem.Status.Value;
        await ctx.Response.WriteAsJsonAsync(problem, token);
        return true;
    }
}
```

```csharp
// Program.cs
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
// ...
app.UseExceptionHandler();
```

## Do / Don't

- **Do** map `Error.Type` to the specific HTTP status; keep the mapping in one shared place.
- **Do** return `ProblemDetails` for every error response.
- **Do** attach per-field details for validation (`400`) failures.
- **Do** reserve `422` for semantic business-rule rejections only.
- **Don't** map every failure to `422` (or any single status).
- **Don't** put try/catch or status decisions in controllers.
- **Don't** leak exception messages, stack traces, or internal identifiers to clients.
