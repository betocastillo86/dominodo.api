# 04 — Validation

## What it is

There are two distinct kinds of validation, and they live in different places:

1. **Input validation** — is the request itself well-formed? (required fields present, lengths,
   formats, ranges). Handled by **FluentValidation** validators, run automatically by the
   `ValidationBehavior` before the handler executes.
2. **Domain invariants** — is this operation allowed given the current state of the model?
   (e.g. "a closed PQR cannot be closed again"). Enforced **inside the aggregate**, returned as a
   `Result` with an `Error`.

Keeping them separate means the handler can assume its input is structurally valid and focus purely
on business rules.

## Why

- Input validation is generic and mechanical; running it in a behavior removes boilerplate from every
  handler and guarantees it always runs.
- Domain invariants depend on state the validator cannot see; they belong to the aggregate that owns
  that state, so the model can never be driven into an invalid condition.
- Both surface to the API through the same `Result`/`Error` channel, so the HTTP mapping is uniform
  (see [08 — Error Handling](./08-error-handling.md)).

## Input validation with FluentValidation

A validator is colocated with its command/query and is `internal`. It only checks the shape of the
request — never database state, never cross-module facts.

```csharp
// Dominodo.Pqrs.Application/Pqrs/OpenPqr/OpenPqrCommand.cs
internal sealed class OpenPqrCommandValidator : AbstractValidator<OpenPqrCommand>
{
    public OpenPqrCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
    }
}
```

Validators are discovered per module and registered from the module assembly:

```csharp
services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
```

## The ValidationBehavior

The behavior collects every registered validator for the request type, runs them, and — if any fail —
short-circuits with a **validation `Result`** carrying the field errors. It does **not** throw for
expected validation failures; the failure is a value that flows back through the pipeline and is
mapped to `400 Bad Request` at the edge.

```csharp
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => new ValidationError(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (failures.Count == 0)
            return await next();

        // Return a failed Result instead of throwing. Shape it so the edge can map it to 400.
        return (TResponse)ValidationResultFactory.Create<TResponse>(failures);
    }
}

public sealed record ValidationError(string Property, string Message);
```

`ValidationResultFactory` builds a `Result`/`Result<T>` whose `Error.Type` is `Validation` and whose
extension data carries the per-field `ValidationError` list. The edge mapper turns that into an
RFC 9457 `ProblemDetails` with `status = 400` and an `errors` dictionary
(see [08 — Error Handling](./08-error-handling.md)).

> Why return instead of throw? Expected failures are part of the contract of a handler. Throwing for
> them turns control flow into exception handling and makes every call site guess what might be
> thrown. A returned `Result` keeps the outcome explicit and cheap.

## Domain invariants stay in the aggregate

The validator confirms `Subject` is present and short enough. Whether the PQR *can be closed* is a
rule about the aggregate's current state, so it lives on the aggregate and returns a `Result`:

```csharp
public Result Close(IClock clock)
{
    if (Status == PqrStatus.Closed)
        return Error.Conflict("Pqr.AlreadyClosed", "The PQR is already closed.");
    // ...
    return Result.Success();
}
```

This surfaces as `409 Conflict`, not `400` — the request was well-formed, but the state disallows it.

## Choosing where a rule goes

| Question the rule answers | Where it lives | Resulting HTTP status |
| --- | --- | --- |
| Is the field present / the right length / a valid format? | FluentValidation validator | `400 Bad Request` |
| Is this value within an allowed static range? | FluentValidation validator | `400 Bad Request` |
| Does this referenced thing exist? | handler (repository / module facade) | `404 Not Found` |
| Is this operation allowed given current state? | aggregate method → `Result` | `409 Conflict` / `422` |
| Is the caller allowed to do this? | handler / authorization | `403 Forbidden` |

## Do / Don't

- **Do** keep validators limited to the shape of the request.
- **Do** enforce state-dependent rules inside the aggregate and return `Result`.
- **Do** rely on the behavior to run validation — never call a validator by hand in a handler.
- **Don't** query the database or another module from a validator.
- **Don't** throw exceptions for expected validation or business-rule failures.
- **Don't** duplicate a domain invariant as a validator rule; it will drift out of sync.
