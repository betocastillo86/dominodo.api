# 01 — Modular Monolith

## What it is

Dominodo ships as a **single deployable process** (one API host), but internally it is divided into
**modules**. Each module is a bounded context — a self-contained slice of the domain with its own
model, its own use cases, its own database schema, and its own public contract. Modules do not
reach into each other's internals; they collaborate only through explicit, published surfaces.

This gives us the productivity of a monolith (one build, one deploy, in-process calls, local
transactions inside a module) while keeping the seams of a distributed system. When a module needs
to become its own service — for scaling, team ownership, or isolation — the seams are already in
place and extraction is a mechanical change, not a rewrite.

## Why

- **Low friction now.** No network hops, no cross-service versioning, no distributed debugging while
  the product is still taking shape.
- **Optionality later.** Because modules are already isolated (separate schema, no shared
  transactions, communication only through contracts and events), a module can be lifted out without
  untangling a big ball of mud.
- **Boundaries force good design.** Being unable to `new` up another module's entity or query its
  tables pushes every interaction through a deliberate contract.

## Module anatomy — five projects

Every module is its own miniature clean architecture, expressed as five projects:

```
Modules/Pqrs/
  Dominodo.Pqrs.Domain          # the model: aggregates, value objects, domain events,
                                #   domain-owned ports (e.g. IPqrRepository)
  Dominodo.Pqrs.Application     # use cases: commands/queries + handlers, validators,
                                #   application ports, the INTERNAL implementation of the facade.
                                #   No ASP.NET, no Shared.Infrastructure.
  Dominodo.Pqrs.Api             # inbound HTTP adapter: controllers ONLY (dispatch the Application's
                                #   internal commands via ISender, granted by InternalsVisibleTo)
  Dominodo.Pqrs.Contracts       # the PUBLIC surface: integration events + IPqrsModuleApi
  Dominodo.Pqrs.Persistence     # the module's own adapter: PqrsDbContext (schema `pqrs`),
                                #   repositories, EF configurations, migrations
```

### Domain

Pure business model. No EF, no HTTP, no MediatR, no external SDKs. Contains aggregates, value
objects, domain events, and the **domain-owned ports** the model needs expressed as interfaces
(for example `IPqrRepository`). References only `Shared.Kernel`.

### Application

The use-case layer. Holds CQRS commands, queries, their handlers, and FluentValidation validators.
Defines **application-owned ports** — interfaces describing what this module needs from the outside
world when the need is specific to the module. Also contains the **internal implementation** of the
module's public facade (`IPqrsModuleApi`), which delegates to the module's own MediatR.

Everything here is `internal`. Other modules cannot see or dispatch these requests. It references
neither `Shared.Infrastructure` nor ASP.NET — its cross-cutting plumbing (the MediatR
validation/UoW/logging behaviors) lives in `Shared.Application`.

### Api

The module's **inbound HTTP adapter** — its controllers, and nothing else. It references its own
`Application` plus `Shared.Infrastructure` (for HTTP helpers like `ErrorResults.ToProblem`) and takes a
`FrameworkReference` to `Microsoft.AspNetCore.App`. Controllers dispatch the `Application`'s `internal`
MediatR commands via `ISender`; access is granted by an `InternalsVisibleTo` on `Application`, so the
requests stay `internal`. The host registers this assembly via `AddApplicationPart`.

### Contracts

The **only** project other modules are allowed to reference. It is deliberately thin and stable:

- **Integration events** — the messages this module publishes when something noteworthy happens
  (`PqrClosedIntegrationEvent`).
- **The module facade** — `IPqrsModuleApi`, the synchronous read surface other modules call.
- **Public DTOs** — the data shapes returned by the facade or carried by events.

No behavior, no domain logic, no EF types. If it isn't meant for another module to consume, it does
not belong here.

### Persistence

The module's own outbound adapter for storage. Owns `PqrsDbContext`, mapped to the `pqrs` schema,
plus repository implementations and EF `IEntityTypeConfiguration` classes. This is a **per-module
adapter**, not shared infrastructure — see [05 — Ports & Adapters](./05-ports-and-adapters.md).

## Public vs internal — the boundary in one picture

```
        ┌─────────────────────────── Module: Pqrs ───────────────────────────┐
        │                                                                     │
 other  │   Contracts  ◀───────────────  Application  ───────▶  Domain        │
modules ─┼─▶ IPqrsModuleApi              (internal handlers)     (aggregates)  │
 see ────┘   integration events                │                              │
 ONLY this   public DTOs                        ▼                             │
        │                                  Persistence (schema `pqrs`)         │
        └─────────────────────────────────────────────────────────────────────┘
```

- Inbound to the module: HTTP controllers (in `<Module>.Api`, hosted by `Dominodo.Api`) and message
  consumers translate external requests into MediatR commands/queries.
- Outbound from the module: integration events (async) and its published DTOs.
- Everything between `Contracts` and `Persistence` is invisible to the rest of the system.

## Communicating between modules

Two channels, chosen by intent — detailed in
[07 — Inter-Module Communication](./07-inter-module-communication.md):

- **Write / notify → integration events (async).** "Something happened; whoever cares can react."
  Published through the message bus, backed by a per-module transactional outbox.
- **Read → `IModuleApi` (sync, in-process).** "I need a fact from you right now." A normal .NET
  method call through the other module's `Contracts` interface.

**Never** reference another module's `Domain`, `Application`, or `Persistence`. **Never** query
another module's schema. **Never** open a transaction that spans two modules.

## How a module becomes a microservice

Because the seams already exist, extraction is mechanical:

1. **Reads.** Replace the in-process `IPqrsModuleApi` implementation with an HTTP/gRPC client that
   implements the same interface. Consumers are unchanged — they still depend on the interface from
   `Contracts`.
2. **Writes.** The module already publishes integration events over Wolverine; switch the transport
   from durable local queues to a real broker (config only). Handlers keep subscribing to the same event types.
3. **Data.** The module already owns its schema with no cross-module foreign keys, so its tables move
   to a dedicated database as-is.
4. **Host.** Give the module its own host project that references its five projects and wires the
   same DI. The composition root is the only place that changes shape.

The application code inside the module — domain, handlers, validators — does not change.

## Do / Don't

- **Do** keep `Contracts` small, stable, and free of behavior.
- **Do** make commands, queries, handlers, and the facade implementation `internal`.
- **Do** treat each module as if it already lived in a separate process.
- **Don't** add a foreign key, a join, or a shared table across modules.
- **Don't** reference another module's non-`Contracts` project "just for a type" — publish the type
  in `Contracts` or duplicate the small DTO.
- **Don't** wrap a command in one module and a command in another in a single transaction.
