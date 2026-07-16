# 02 — DDD Building Blocks

## What it is

The domain layer is where business rules live, expressed with a small, shared vocabulary of base
types kept in `Dominodo.Shared.Kernel`. Every module builds its model from the same primitives:
**entities**, **aggregate roots**, **value objects**, **domain events**, and the **`Result`/`Error`**
types used to report outcomes without throwing for expected failures.

## Why

- A consistent kernel means every module's model reads the same way.
- Aggregates protect invariants: state can only change through methods that enforce the rules, so an
  aggregate can never be observed in an invalid state.
- Domain events let the model announce facts without knowing who listens.
- `Result`/`Error` make expected failures part of the method signature instead of hidden exceptions.

## The kernel

### `Entity`

An object with identity and a lifecycle. Equality is by `Id`, not by value. Entities can raise
domain events.

```csharp
namespace Dominodo.Shared.Kernel;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected Entity(Guid id) => Id = id;
    protected Entity() { } // EF Core

    public Guid Id { get; protected init; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id == other.Id;
    public override int GetHashCode() => (GetType(), Id).GetHashCode();
}
```

### `AggregateRoot`

The entry point to an aggregate — the only object outside code loads, saves, and mutates. It is the
consistency boundary: invariants that must always hold are enforced here. Repositories are defined
**per aggregate root**, never per child entity.

```csharp
namespace Dominodo.Shared.Kernel;

public abstract class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id) : base(id) { }
    protected AggregateRoot() { }
}
```

Auditing fields (`CreatedAtUtc`, `CreatedBy`, `UpdatedAtUtc`, `UpdatedBy`) are **not** written by
hand — they are applied automatically by an EF interceptor (see
[06 — Persistence](./06-persistence.md)). Multi-tenant aggregates carry a `TenantId`
(see [09 — Multitenancy](./09-multitenancy.md)).

### `ValueObject`

An immutable concept with no identity, compared by its values. Use value objects to make illegal
states unrepresentable (an `Email` that is always well-formed, a `Money` that always has a currency).

```csharp
namespace Dominodo.Shared.Kernel;

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj) =>
        obj is ValueObject other &&
        GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(0, (hash, c) => HashCode.Combine(hash, c?.GetHashCode() ?? 0));
}
```

```csharp
public sealed class Email : ValueObject
{
    private Email(string value) => Value = value;
    public string Value { get; }

    public static Result<Email> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.Contains('@'))
            return Error.Validation("Email.Invalid", "Email is not a valid address.");
        return new Email(input.Trim().ToLowerInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
}
```

### Domain events

A record of something that happened **inside a single module**. Raised by an aggregate; when the
command commits, the unit of work persists it to the module's Wolverine outbox in the **same
transaction** as the aggregate, and it is delivered **async/durable** to an in-module Wolverine handler
(see [03 — CQRS](./03-cqrs-mediatr.md) and
[07 — Inter-Module Communication](./07-inter-module-communication.md)). Domain events never cross
module boundaries — that is what integration events are for.

```csharp
namespace Dominodo.Shared.Kernel;

public interface IDomainEvent; // plain marker — NOT a MediatR notification; routed by Wolverine
```

```csharp
// Dominodo.Pqrs.Domain
public sealed record PqrClosedDomainEvent(Guid PqrId, DateTimeOffset ClosedAtUtc) : IDomainEvent;
```

### `Result` and `Error`

Expected failures (validation, not-found, conflict, broken business rule) are returned, not thrown.
Exceptions are reserved for the truly unexpected. `Error` carries an error **type** that the API
layer maps to the correct HTTP status — see [08 — Error Handling](./08-error-handling.md).

```csharp
namespace Dominodo.Shared.Kernel;

public enum ErrorType { Validation, NotFound, Conflict, Forbidden, Unauthorized, Failure }

public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error NotFound(string code, string description)   => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description)   => new(code, description, ErrorType.Conflict);
    public static Error Forbidden(string code, string description)  => new(code, description, ErrorType.Forbidden);
    public static Error Failure(string code, string description)    => new(code, description, ErrorType.Failure);
}
```

```csharp
namespace Dominodo.Shared.Kernel;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None) throw new InvalidOperationException();
        if (!isSuccess && error == Error.None) throw new InvalidOperationException();
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;
    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
```

## Putting it together — an aggregate that enforces its invariants

```csharp
// Dominodo.Pqrs.Domain
public sealed class Pqr : AggregateRoot
{
    private Pqr() { } // EF Core

    private Pqr(Guid id, Guid tenantId, Guid apartmentId, string subject, string body) : base(id)
    {
        TenantId = tenantId;
        ApartmentId = apartmentId;
        Subject = subject;
        Body = body;
        Status = PqrStatus.Open;
    }

    public Guid TenantId { get; private set; }
    public Guid ApartmentId { get; private set; }
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public PqrStatus Status { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public static Result<Pqr> Open(Guid tenantId, Guid apartmentId, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return Error.Validation("Pqr.SubjectRequired", "Subject is required.");

        return new Pqr(Guid.NewGuid(), tenantId, apartmentId, subject, body);
    }

    public Result Close(IClock clock)
    {
        if (Status == PqrStatus.Closed)
            return Error.Conflict("Pqr.AlreadyClosed", "The PQR is already closed.");

        Status = PqrStatus.Closed;
        ClosedAtUtc = clock.UtcNow;
        Raise(new PqrClosedDomainEvent(Id, ClosedAtUtc.Value));
        return Result.Success();
    }
}
```

## Do / Don't

- **Do** keep the domain free of framework types (no EF attributes, no `DbContext`, no `IMediator`).
- **Do** change state only through aggregate methods that enforce invariants; keep setters `private`.
- **Do** define one repository per aggregate root.
- **Do** return `Result`/`Error` for expected failures; reserve exceptions for bugs and truly
  exceptional conditions.
- **Don't** raise a domain event for something another module needs — that is an integration event
  (see [07](./07-inter-module-communication.md)).
- **Don't** let a value object be mutable or comparable by reference.
- **Don't** load or mutate a child entity outside its aggregate root.
