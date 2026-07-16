# 03 — CQRS with MediatR

## What it is

Every use case is a **command** (it changes state) or a **query** (it reads state). Each is a small
request object handled by exactly one handler, dispatched through MediatR. Cross-cutting concerns —
validation, logging, saving the unit of work — live in **pipeline behaviors** that wrap every
handler, so handlers stay focused on business logic.

MediatR is an **internal implementation detail of each module**. Requests and handlers are
`internal`; no other module can construct or dispatch them. Cross-module calls go through the
module facade instead (see [07 — Inter-Module Communication](./07-inter-module-communication.md)).

## Why

- One handler per use case keeps each unit small, testable, and easy to find.
- Commands vs queries separate the write model (which enforces invariants) from the read model
  (which can be shaped freely for the caller).
- Behaviors remove repetitive plumbing from handlers and apply it uniformly.
- Keeping MediatR internal preserves module boundaries: a use case is reachable only through the
  module's own inbound adapters (controllers, consumers) or its public facade.

## Abstractions

Thin markers over MediatR give us intent-revealing names and let behaviors target the right requests.
They live in `Shared.Kernel` (or a small `Shared.Application` package) so every module uses the same.

```csharp
namespace Dominodo.Shared.Kernel.Messaging;

public interface ICommand : IRequest<Result>;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
```

Every handler returns a `Result`/`Result<T>` — expected failures are values, not exceptions
(see [02 — DDD Building Blocks](./02-ddd-building-blocks.md) and [08 — Error Handling](./08-error-handling.md)).

## A command end to end

Request, validator, and handler are colocated per use case (one folder per feature). All `internal`.

```csharp
// Dominodo.Pqrs.Application/Pqrs/OpenPqr/OpenPqrCommand.cs
internal sealed record OpenPqrCommand(Guid ApartmentId, string Subject, string Body) : ICommand<Guid>;

internal sealed class OpenPqrCommandValidator : AbstractValidator<OpenPqrCommand>
{
    public OpenPqrCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty();
    }
}

internal sealed class OpenPqrCommandHandler(
    IPqrRepository repository,
    ITenantContext tenant,
    ITenantsModuleApi tenants) // sync read into another module — via its Contracts facade
    : ICommandHandler<OpenPqrCommand, Guid>
{
    public async Task<Result<Guid>> Handle(OpenPqrCommand command, CancellationToken ct)
    {
        // cross-module read: confirm the apartment exists in the Tenants module
        var apartment = await tenants.GetApartmentAsync(command.ApartmentId, ct);
        if (apartment is null)
            return Error.NotFound("Apartment.NotFound", "Apartment does not exist.");

        var result = Pqr.Open(tenant.TenantId, command.ApartmentId, command.Subject, command.Body);
        if (result.IsFailure)
            return result.Error;

        repository.Add(result.Value);
        // NOTE: no SaveChangesAsync here — the UnitOfWorkBehavior commits the transaction.
        return result.Value.Id;
    }
}
```

## A query

Queries bypass the aggregate and project straight into a DTO shaped for the caller. They should be
read-only and, where useful, use `AsNoTracking()`.

```csharp
// Dominodo.Pqrs.Application/Pqrs/GetPqrById/GetPqrByIdQuery.cs
internal sealed record GetPqrByIdQuery(Guid PqrId) : IQuery<PqrDto>;

internal sealed class GetPqrByIdQueryHandler(IPqrsReadContext db, ITenantContext tenant)
    : IQueryHandler<GetPqrByIdQuery, PqrDto>
{
    public async Task<Result<PqrDto>> Handle(GetPqrByIdQuery query, CancellationToken ct)
    {
        var dto = await db.Pqrs
            .ForCurrentTenant(tenant)                 // explicit tenant scoping — see doc 09
            .Where(p => p.Id == query.PqrId)
            .Select(p => new PqrDto(p.Id, p.Subject, p.Status.ToString()))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Error.NotFound("Pqr.NotFound", "PQR not found.")
            : dto;
    }
}
```

## Pipeline behaviors

Behaviors wrap every request in a defined order. They live in `Shared.Infrastructure` and are
registered once for all modules.

### ValidationBehavior

Runs all registered FluentValidation validators for the request before the handler. On failure it
short-circuits with a validation `Result` (it does **not** throw for expected validation errors) —
see [04 — Validation](./04-validation.md).

### LoggingBehavior

Emits a structured log line at the start and end of each request, including the outcome and the
`Error.Code` on failure. Correlates via the ambient correlation id
(see [11 — Cross-Cutting](./11-cross-cutting.md)).

```csharp
internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {Request}", name);
        var response = await next();
        if (response.IsSuccess) logger.LogInformation("Handled {Request}", name);
        else logger.LogWarning("Request {Request} failed: {ErrorCode}", name, response.Error.Code);
        return response;
    }
}
```

### UnitOfWorkBehavior

Wraps **command** handlers in a transaction so handlers never call `SaveChangesAsync` themselves. It
commits when the command completes; an **exception** (a true abort) propagates past the behavior, so
no commit happens. An expected-failure `Result` is a normal outcome — not a rollback signal — so it
**still commits** any state the handler deliberately recorded (e.g. a failed OTP attempt that must be
counted). The convention is: handlers **guard-first, mutate-last, and throw to abort**. Committing is
also where each module's domain events are dispatched and its outbox is flushed
(see [07](./07-inter-module-communication.md)). Queries are not wrapped.

Because each module owns its own `DbContext`/`IUnitOfWork`, the behavior resolves **all** registered
units of work and saves each. A command only mutates its own module's context; the others have no
tracked changes, so their `SaveChanges` is a no-op — modules never share a transaction.

```csharp
internal sealed class UnitOfWorkBehavior<TRequest, TResponse>(IEnumerable<IUnitOfWork> unitsOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not IBaseCommand) // commands only (marker shared by ICommand and ICommand<T>)
            return await next();

        var response = await next();              // throws → propagates, no commit (rollback)
        foreach (var unitOfWork in unitsOfWork)   // each module's DbContext; unchanged ones no-op
            await unitOfWork.SaveChangesAsync(ct); // dispatches domain events + flushes outbox
        return response;
    }
}
```

Registration order matters — behaviors execute outermost-first:

```
Logging → Validation → UnitOfWork → Handler
```

## Registration (per module)

Each module exposes an `Add<Module>Module` extension that the host calls. It registers the module's
MediatR handlers and validators from that assembly.

```csharp
// Dominodo.Pqrs.Application/DependencyInjection.cs
public static IServiceCollection AddPqrsModule(this IServiceCollection services, IConfiguration config)
{
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
    services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

    services.AddScoped<ITenantsModuleApi... >(); // if this module owns a facade, register its impl
    services.AddPqrsPersistence(config);          // registers PqrsDbContext + repositories
    return services;
}
```

Shared behaviors are registered once by the host over the open generic pipeline:

```csharp
// Dominodo.Shared.Infrastructure
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
```

## Do / Don't

- **Do** keep one command/query + handler per use case, colocated in a feature folder.
- **Do** make requests and handlers `internal`.
- **Do** let the `UnitOfWorkBehavior` own the transaction; handlers add/modify aggregates and return.
- **Do** return `Result`/`Result<T>` from every handler.
- **Don't** dispatch another module's request; call its facade from `Contracts`.
- **Don't** put cross-cutting concerns (logging, saving, validation) inside a handler.
- **Don't** mutate state in a query handler.
