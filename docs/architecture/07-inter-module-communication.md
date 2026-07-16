# 07 — Inter-Module Communication

> **Bus: Wolverine** (MIT). Replaced MassTransit, which went commercial at v9 (paid license to boot)
> and whose EF outbox didn't work in-process. Wolverine's durable outbox works in-process today and
> its v5 modular-monolith support fits our one-DbContext/one-schema-per-module model. MediatR stays
> for in-module dispatch of commands/queries; domain events go through Wolverine's durable outbox.

## What it is

Modules collaborate through exactly two channels, chosen by intent:

| Intent | Channel | Sync? | Coupling |
| --- | --- | --- | --- |
| "Something happened; whoever cares can react." | **Integration event** over the message bus | async | temporal decoupling |
| "I need a fact from you right now." | **`IModuleApi` facade** call | sync, in-process | call-time |

A third concept is **not** cross-module: **domain events**, delivered async/durable *within a single
module* through that module's own outbox, never leaving it.

## Why the three-way split

- **Domain events** decouple a module's internals (an aggregate announces a fact; local handlers
  react). They are persisted to the module's own outbox in the same transaction as the aggregate, then
  delivered async — so they stay within the module (its schema), never reaching another module.
- **Integration events** are the mechanism when another module (another schema, another future
  service) must react. Publisher and subscriber commit **separate** transactions to **separate**
  schemas — eventual consistency, exactly the microservice semantics.
- **The facade** covers a handler needing a fact *now* (does this apartment exist?). A synchronous
  in-process call through a `Contracts` interface becomes a network client for free on extraction.

## Domain events (intra-module)

Raised by an aggregate (`IDomainEvent` is a plain marker — **not** a MediatR `INotification`).
When the command's `UnitOfWorkBehavior` saves, the module's unit of work
(`WolverineUnitOfWork<TDbContext>`) collects the events raised by the tracked aggregates, publishes
them to the module's Wolverine outbox, and calls `SaveChangesAndFlushMessagesAsync` — so the aggregate
mutation and the outbox rows commit in **one transaction**, then the events are delivered
**async/durable** to in-module **Wolverine** handlers. The immediate in-process dispatch is gone (the
accepted trade-off): a handler runs *after* the commit, with retry/durability. A common pattern: a
domain-event handler **translates** an internal fact into a published integration event.

```csharp
// raised inside the aggregate (Dominodo.Pqrs.Domain)
Raise(new PqrClosedDomainEvent(Id, ClosedAtUtc.Value));

// handled inside the same module (Dominodo.Pqrs.Application) — a Wolverine handler, delivered async
// from the outbox after commit. Like the integration-event consumers: **public** (Wolverine's
// generated code cannot see internal types) but treated like a controller — it only dispatches this
// module's OWN work, so the boundary holds. Dependencies as METHOD parameters (constructor injection
// trips ServiceLocationPolicy). Translates to an integration event; that PublishAsync enrols in this
// module's outbox too.
public sealed class WhenPqrClosed_PublishIntegrationEvent
{
    public Task Handle(PqrClosedDomainEvent e, IMessageBus bus, CancellationToken ct) =>
        bus.PublishAsync(new PqrClosedIntegrationEvent(e.PqrId, e.ClosedAtUtc)).AsTask();
}
```

Register domain-event handlers explicitly per module (e.g. `discovery.IncludeType<...>()`, like
`AddAdminHandlers`) — the same mechanism as integration-event consumers.

## Integration events (cross-module) — the reliable flow

Module A commits to schema `a`; module B must react and commit to schema `b`. Never lose an event if A
commits then crashes; never publish for a change that rolled back. The **transactional outbox** solves
both.

```
Module A (tx on schema `a`)                    Module B (tx on schema `b`)
1. Handler mutates its aggregate
2. Domain event → handler builds the integration event
3. PublishAsync → OUTBOX  ── SAME tx ──▶  commit: business change + outbox row are atomic
4. Wolverine durability agent reads the outbox,
   delivers via durable local queue (broker later = config)  ──▶  5. B's handler receives it
                                                                  6. B commits in ITS OWN tx
                                                                     (retry on failure; inbox +
                                                                      natural key = idempotent)
```

- **Atomic publish** — outbox row written in A's business transaction; publish can't diverge from state.
- **At-least-once** — Wolverine retries, so handlers must be **idempotent** (inbox dedupe + natural key).

## Setup

One EF-integrated `DbContext` + one ancillary message store per module (each keeps its own schema).
Durable **local queues** are the in-process transport today; swapping to RabbitMQ / Azure Service Bus
is config only.

```csharp
// Program.cs — registered once by the host
builder.Host.UseWolverine((context, opts) =>
{
    var cs = context.Configuration.GetConnectionString("Default")!;

    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;   // each module: own tx + retry
    opts.Durability.MessageIdentity = MessageIdentity.IdAndDestination; // same event, many modules
    opts.Durability.MessageStorageSchemaName = "wolverine";            // bus storage, separate schema

    // one enrolled DbContext + ancillary message store per module
    opts.Services.AddDbContextWithWolverineIntegration<UsersDbContext>(x => x.UseSqlServer(cs));
    opts.PersistMessagesWithSqlServer(cs, role: MessageStoreRole.Ancillary).Enroll<UsersDbContext>();
    opts.Services.AddDbContextWithWolverineIntegration<AdminDbContext>(x => x.UseSqlServer(cs));
    opts.PersistMessagesWithSqlServer(cs, role: MessageStoreRole.Ancillary).Enroll<AdminDbContext>();

    // discover each module's handlers (they are `internal`)
    opts.Discovery.IncludeAssembly(typeof(Users.Application.DependencyInjection).Assembly);
    opts.Discovery.IncludeAssembly(typeof(Admin.Application.DependencyInjection).Assembly);
    opts.Discovery.CustomizeHandlerDiscovery(x => x.Includes.IsNotPublic());

    opts.Policies.UseDurableLocalQueues(); // swap to opts.UseRabbitMq(...) later — handlers unchanged
});
```

Notes:
- **Internal handlers** need `CustomizeHandlerDiscovery(x => x.Includes.IsNotPublic())` (Wolverine
  discovers public types by default). Verify the exact signature against the Wolverine 5 version.
- **One `DbContext` per handler** under the EF middleware — matches our rule (a handler touches only
  its own module's `DbContext`).
- No MassTransit `OnModelCreating` calls (`AddInboxStateEntity`/`AddOutboxMessageEntity`/…); storage is
  set up by `AddDbContextWithWolverineIntegration<T>()` (or `modelBuilder.MapWolverineEnvelopeStorage()`).

## Contracts

Integration events live in the publishing module's `Contracts` — the only place others look. Flat,
versioned by addition, free of domain types.

```csharp
// Dominodo.Pqrs.Contracts/IntegrationEvents/PqrClosedIntegrationEvent.cs
public sealed record PqrClosedIntegrationEvent(Guid PqrId, DateTimeOffset ClosedAtUtc);
```

## Consuming in another module

A handler is an inbound adapter that translates the event into a command on the consuming module's own
MediatR. No `IConsumer<T>` — Wolverine binds by convention (a `Handle`/`Consume` method whose first
parameter is the message; dependencies injected as parameters). Keep it `internal` and idempotent.

```csharp
// Dominodo.Notifications.Application/Consumers/PqrClosedHandler.cs
internal static class PqrClosedHandler
{
    public static Task Handle(PqrClosedIntegrationEvent message, ISender sender, CancellationToken ct) =>
        sender.Send(new NotifyResidentPqrClosedCommand(message.PqrId), ct); // idempotent command
}
```

When a command handler owns the `DbContext` and wants an explicit atomic publish, use
`IDbContextOutbox<TDbContext>`: mutate, `PublishAsync(...)`, then `SaveChangesAndFlushMessagesAsync()`.

## Synchronous reads — the module facade

When a handler needs a fact from another module *now*, it calls that module's facade — a plain .NET
interface in `Contracts`; MediatR stays private to each module.

```csharp
// Dominodo.Tenants.Contracts/ITenantsModuleApi.cs  (PUBLIC)
public interface ITenantsModuleApi
{
    Task<ApartmentDto?> GetApartmentAsync(Guid id, CancellationToken ct);
}
public sealed record ApartmentDto(Guid Id, string Number, Guid TenantId);

// Dominodo.Tenants.Application/TenantsModuleApi.cs  (INTERNAL impl — uses this module's own MediatR)
internal sealed class TenantsModuleApi(ISender sender) : ITenantsModuleApi
{
    public async Task<ApartmentDto?> GetApartmentAsync(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetApartmentByIdQuery(id), ct);
        return result.IsSuccess ? result.Value : null;
    }
}
```

The other module's query is `internal` — it can't be named from outside. The facade is the only door,
and it is the door that becomes a remote client later.

## Extraction later — the seam in action

- **Reads:** provide `TenantsHttpClient : ITenantsModuleApi` in the new service's client library;
  register it instead of `TenantsModuleApi`. Callers unchanged.
- **Writes:** switch the Wolverine transport from durable local queues to a broker
  (`opts.UseRabbitMq(...)`); event types and handlers unchanged.

## Do / Don't

- **Do** use integration events when another module must react.
- **Do** publish through the outbox — never fire-and-forget. Domain events reach it automatically via
  the unit of work (`WolverineUnitOfWork<T>` → `IDbContextOutbox<T>` + `SaveChangesAndFlushMessagesAsync()`,
  same tx as the aggregate); integration events via `IMessageBus.PublishAsync` in the command/handler.
- **Do** make every handler idempotent (inbox + natural key).
- **Do** use the facade for synchronous cross-module reads.
- **Don't** use a domain event to reach another module.
- **Don't** call another module's `DbContext`, repository, or handler directly.
- **Don't** put domain entities inside integration events or facade DTOs — publish flat contract types.
