# 07 — Inter-Module Communication

## What it is

Modules collaborate through exactly two channels, chosen by intent:

| Intent | Channel | Sync? | Coupling |
| --- | --- | --- | --- |
| "Something happened; whoever cares can react." | **Integration event** over the message bus | async | temporal decoupling |
| "I need a fact from you right now." | **`IModuleApi` facade** call | sync, in-process | call-time |

There is a third concept that is **not** cross-module: **domain events**, which are dispatched
in-process *within a single module* and never leave it.

## Why the three-way split

- **Domain events** keep a module's own internals decoupled (an aggregate announces a fact; local
  handlers react) but they run in the same transaction and the same database — so they cannot be the
  mechanism for reaching a module that owns a different schema.
- **Integration events** are the correct mechanism when another module (another schema, another
  future service) must react. Publisher and subscriber commit **separate** transactions to their
  **separate** schemas — this is eventual consistency, exactly the semantics we would have between
  microservices.
- **The facade** covers the case where a handler needs a fact immediately (does this apartment
  exist?). A synchronous in-process call is simplest and, because it goes through an interface in
  `Contracts`, it becomes a network client for free when the module is extracted.

## Domain events (intra-module)

Raised by an aggregate, dispatched after `SaveChangesAsync` by the
`DispatchDomainEventsInterceptor`, handled by in-process MediatR `INotificationHandler`s inside the
same module and the same transaction.

```csharp
// raised inside the aggregate (Dominodo.Pqrs.Domain)
Raise(new PqrClosedDomainEvent(Id, ClosedAtUtc.Value));

// handled inside the same module (Dominodo.Pqrs.Application)
internal sealed class WhenPqrClosed_PublishIntegrationEvent(IPublishEndpoint bus)
    : INotificationHandler<PqrClosedDomainEvent>
{
    public Task Handle(PqrClosedDomainEvent e, CancellationToken ct) =>
        bus.Publish(new PqrClosedIntegrationEvent(e.PqrId, e.ClosedAtUtc), ct);
}
```

A very common pattern (above): a domain event handler is what **translates** an internal fact into a
published integration event.

## Integration events (cross-module) — the reliable flow

The challenge: module A commits to schema `a`, module B must react and commit to schema `b`. We must
never lose the event if A commits and then crashes, and we must never publish an event for a change
that rolled back. The **transactional outbox** solves both.

```
Module A  (transaction on schema `a`)              Module B (transaction on schema `b`)
─────────────────────────────────────             ─────────────────────────────────────
1. Handler mutates its aggregate
2. Domain event → in-process handler
   builds the integration event
3. bus.Publish(...) writes the event to
   A's OUTBOX table  ── SAME transaction ──▶  commit: business change + outbox row are atomic
                                                     │
4. MassTransit delivery service reads A's           │  (in-memory bus now; a real broker later —
   outbox and hands the message to the bus  ────────┘   config only, no code change)
                                                     ▼
                                            5. B's consumer receives the message
                                            6. B does its work in ITS OWN transaction, commits
                                               (on failure → MassTransit retries; use an inbox /
                                                idempotent handler to dedupe — see below)
```

Key properties:

- **A and B never share a transaction.** Correct — they do not share a database. Consistency between
  them is *eventual*.
- **Atomic publish.** The outbox row is written in the same transaction as A's business change, so
  publishing can't diverge from the state change.
- **At-least-once delivery.** MassTransit retries failed consumers, so B's consumer must be
  **idempotent** (safe to process the same event twice).

### Setup

MassTransit provides both the bus abstraction and an **EF Core transactional outbox**, configured
**per module `DbContext`**. The in-memory transport is used today; switching to RabbitMQ / Azure
Service Bus later is a configuration change.

```csharp
// registered by the host, once
services.AddMassTransit(x =>
{
    // discover consumers from each module's Application assembly
    x.AddConsumers(typeof(Pqrs.Application.DependencyInjection).Assembly);
    x.AddConsumers(typeof(Notifications.Application.DependencyInjection).Assembly);

    // per-module outbox on that module's DbContext
    x.AddEntityFrameworkOutbox<PqrsDbContext>(o => { o.UsePostgres(); o.UseBusOutbox(); });
    x.AddEntityFrameworkOutbox<NotificationsDbContext>(o => { o.UsePostgres(); o.UseBusOutbox(); });

    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context)); // ← swap transport later
});
```

### Contracts

Integration events are declared in the publishing module's `Contracts` project — the only place other
modules look. Keep them flat, versioned by addition, and free of domain types.

```csharp
// Dominodo.Pqrs.Contracts/IntegrationEvents/PqrClosedIntegrationEvent.cs
public sealed record PqrClosedIntegrationEvent(Guid PqrId, DateTimeOffset ClosedAtUtc);
```

### Consuming in another module

A consumer is an inbound adapter: it translates the event into a command dispatched through the
consuming module's own MediatR. It must be idempotent.

```csharp
// Dominodo.Notifications.Application/Consumers/PqrClosedConsumer.cs
internal sealed class PqrClosedConsumer(ISender sender) : IConsumer<PqrClosedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PqrClosedIntegrationEvent> ctx) =>
        sender.Send(new NotifyResidentPqrClosedCommand(ctx.Message.PqrId)); // idempotent handler
}
```

## Synchronous reads — the module facade

When a handler needs a fact from another module *now*, it calls that module's facade. The facade is a
plain .NET interface in `Contracts`; MediatR stays private to each module.

```csharp
// Dominodo.Tenants.Contracts/ITenantsModuleApi.cs  (PUBLIC)
public interface ITenantsModuleApi
{
    Task<ApartmentDto?> GetApartmentAsync(Guid id, CancellationToken ct);
}
public sealed record ApartmentDto(Guid Id, string Number, Guid TenantId);
```

```csharp
// Dominodo.Tenants.Application/TenantsModuleApi.cs  (INTERNAL implementation)
internal sealed class TenantsModuleApi(ISender sender) : ITenantsModuleApi
{
    public async Task<ApartmentDto?> GetApartmentAsync(Guid id, CancellationToken ct)
    {
        // uses THIS module's own MediatR; the query type is internal to Tenants
        var result = await sender.Send(new GetApartmentByIdQuery(id), ct);
        return result.IsSuccess ? result.Value : null;
    }
}
```

The consumer just injects the interface (shown in [03 — CQRS](./03-cqrs-mediatr.md)):

```csharp
internal sealed class OpenPqrCommandHandler(ITenantsModuleApi tenants, /* ... */) { /* ... */ }
```

**Why not dispatch the other module's query directly?** Because that query type is `internal` to its
module — another module cannot even name it, let alone reference its `Application` assembly. The
facade is the only door, and it is exactly the door that becomes a remote client later.

## Extraction later — the seam in action

- **Reads:** provide `TenantsHttpClient : ITenantsModuleApi` in the new service's client library;
  register it instead of `TenantsModuleApi`. Callers are unchanged.
- **Writes:** flip the MassTransit transport from in-memory to a broker; the same event types route
  over the network. Consumers are unchanged.

## Do / Don't

- **Do** use integration events when another module must react to a change.
- **Do** publish integration events through the outbox (`bus.Publish` inside the command's
  transaction), never as a fire-and-forget after commit.
- **Do** make every consumer idempotent (dedupe by event id / natural key).
- **Do** use the facade for synchronous cross-module reads.
- **Don't** use a domain event to reach another module.
- **Don't** call another module's `DbContext`, repository, or handler directly.
- **Don't** put domain entities inside integration events or facade DTOs — publish flat contract types.
