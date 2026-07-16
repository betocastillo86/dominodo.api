# Dominodo — Architecture & Patterns Reference

This is the authoritative architecture guide for the Dominodo backend. It defines **how we build
things here**: the shape of the solution, the boundaries between modules, and the patterns every
feature is expected to follow. Treat it as a reference to consult on demand — each document is
self-contained and covers one concern end to end.

Dominodo is a **modular monolith**: a single deployable that is internally split into independent
**modules** (bounded contexts). Modules are isolated by design so that any one of them can later be
extracted into its own service with minimal change. Everything in this guide serves that goal —
strong internal boundaries today, cheap extraction tomorrow.

## The five rules everything derives from

1. **A module owns everything inside it and exposes almost nothing.** The only thing another module
   may reference is a module's `Contracts` project. Never its `Domain`, `Application`, or
   `Persistence`.
2. **Modules never share a transaction or a database schema.** Each module owns its own schema and
   its own `DbContext`. There are no foreign keys across module boundaries.
3. **Cross-module writes are asynchronous** (integration events over the message bus).
   **Cross-module reads are synchronous** (a module's public `IModuleApi` interface, called
   in-process).
4. **Dependencies point inward.** `Domain` depends on nothing; `Application` depends on `Domain`;
   adapters (`Persistence`, `Adapters.*`) depend on the abstractions they implement — never the
   other way around.
5. **Boundaries are enforced by tests, not by convention.** `Dominodo.ArchitectureTests` fails the
   build when a rule above is violated.

## Solution layout

```
dominodo.api/
├── Dominodo.sln
├── Directory.Build.props            # nullable, lang version, warnings-as-errors, shared analyzers
├── Directory.Packages.props         # Central Package Management (one place for all versions)
├── src/
│   ├── Bootstrap/
│   │   └── Dominodo.Api             # ASP.NET Core host = composition root.
│   │                                #   The ONLY project that references concrete adapters and
│   │                                #   wires each module via AddXModule().
│   ├── Shared/
│   │   ├── Dominodo.Shared.Kernel          # DDD base types: Entity, AggregateRoot, ValueObject,
│   │   │                                   #   IDomainEvent, Result, Error, ITenantContext
│   │   ├── Dominodo.Shared.Abstractions    # shared ports: IEmailSender, IWhatsAppSender,
│   │   │                                   #   IFileStorage, IPushSender, IClock, JwtOptions
│   │   ├── Dominodo.Shared.Application      # application-layer plumbing: MediatR behaviors
│   │   │                                   #   (validation / UoW / logging). No infra, no ASP.NET
│   │   └── Dominodo.Shared.Infrastructure  # shared plumbing: base DbContext, EF interceptors,
│   │                                       #   Wolverine + outbox wiring (WolverineUnitOfWork),
│   │                                       #   tenancy, ProblemDetails/error mapping, JWT auth
│   ├── Adapters/                    # reusable outbound adapters, one project per external dependency
│   │   ├── Dominodo.Adapters.Email
│   │   ├── Dominodo.Adapters.WhatsApp
│   │   ├── Dominodo.Adapters.Push
│   │   └── Dominodo.Adapters.Storage
│   └── Modules/
│       └── <Module>/
│           ├── Dominodo.<Module>.Domain        # aggregates, value objects, domain events,
│           │                                   #   domain-owned ports (e.g. IPqrRepository)
│           ├── Dominodo.<Module>.Application    # CQRS commands/queries/handlers, validators,
│           │                                   #   application ports, INTERNAL IModuleApi impl.
│           │                                   #   No ASP.NET, no Shared.Infrastructure
│           ├── Dominodo.<Module>.Api            # inbound HTTP adapter: controllers ONLY
│           │                                   #   (dispatch Application internals via InternalsVisibleTo)
│           ├── Dominodo.<Module>.Contracts      # PUBLIC surface: integration events + IModuleApi
│           └── Dominodo.<Module>.Persistence    # the module's own adapter: DbContext + schema,
│                                               #   repositories, EF configurations, migrations
└── tests/
    ├── Dominodo.ArchitectureTests               # NetArchTest: enforces module & layer boundaries (always)
    └── e2e/                                      # standalone black-box HTTP suite (own solution,
                                                  #   zero coupling with src/ — see tests/e2e/README.md)
    # Dominodo.<Module>.UnitTests / .IntegrationTests + Dominodo.TestUtilities are NOT present by
    # default — they are created on demand, only when unit/integration tests are explicitly requested.
```

> **Tests are opt-in.** No test is generated automatically as a side effect of a feature or prompt.
> **Architecture tests** (boundary enforcement, Rule #5) are always maintained; everything else —
> **E2E** and any **unit/integration** coverage — is written **only when explicitly requested**, and
> only for the cases named. Test projects are created on first real need, not scaffolded speculatively.
> See [10 — Testing](./10-testing.md).

## Dependency rules (enforced by architecture tests)

| Project              | May reference                                                        |
| -------------------- | ------------------------------------------------------------------- |
| `*.Domain`           | `Shared.Kernel` only                                                |
| `*.Application`      | own `Domain`, `Shared.Kernel`, `Shared.Abstractions`, `Shared.Application`, other modules' `*.Contracts`. **Not** `Shared.Infrastructure`, **not** ASP.NET |
| `*.Api`              | own `Application`, `Shared.Infrastructure`, `Shared.Kernel`; `FrameworkReference` to ASP.NET. The only module project with controllers |
| `*.Contracts`        | `Shared.Kernel` (kept as thin as possible)                          |
| `*.Persistence`      | own `Application`, own `Domain`, `Shared.Infrastructure`            |
| `Adapters.*`         | `Shared.Abstractions`, `Shared.Kernel`                              |
| `Shared.Application` | `Shared.Kernel` (+ MediatR, FluentValidation). No infra, no ASP.NET |
| `Shared.Infrastructure` | `Shared.Kernel`, `Shared.Abstractions`                           |
| `Dominodo.Api` (host)| everything (composition root only)                                  |

Additional invariants:

- A module may reference another module **only through its `*.Contracts` project**.
- MediatR requests and handlers are `internal`; a module cannot dispatch another module's requests.
- Only `Dominodo.Api` references `Adapters.*` and concrete `*.Persistence` implementations.

## Where things live

| I need to…                                    | Put it in…                                        |
| --------------------------------------------- | ------------------------------------------------- |
| Add business rules / an aggregate             | `<Module>.Domain`                                 |
| Handle a use case (command/query)             | `<Module>.Application`                            |
| Validate incoming request data                | a `AbstractValidator` in `<Module>.Application`   |
| Expose data to another module (read)          | `IModuleApi` in `<Module>.Contracts`              |
| Notify another module of a change (write)     | an integration event in `<Module>.Contracts`     |
| Persist an aggregate                          | `<Module>.Persistence`                            |
| Call an external system (email, WhatsApp, …)  | a shared port in `Shared.Abstractions` + an adapter in `Adapters.*` |
| Add a cross-cutting behavior                  | a MediatR behavior in `Shared.Application`        |
| Expose an HTTP endpoint                        | a controller in `<Module>.Api` (hosted by `Dominodo.Api`) |

## Documents

| # | Document | Covers |
|---|----------|--------|
| — | [README](./README.md) | This index, solution map, dependency rules |
| 01 | [Modular Monolith](./01-modular-monolith.md) | Module anatomy, boundaries, extraction to a service |
| 02 | [DDD Building Blocks](./02-ddd-building-blocks.md) | Entity, AggregateRoot, ValueObject, domain events, Result/Error |
| 03 | [CQRS with MediatR](./03-cqrs-mediatr.md) | Commands, queries, handlers, pipeline behaviors |
| 04 | [Validation](./04-validation.md) | FluentValidation + the ValidationBehavior |
| 05 | [Ports & Adapters](./05-ports-and-adapters.md) | Hexagonal infrastructure, no god Infrastructure project |
| 06 | [Persistence](./06-persistence.md) | Schema-per-module, DbContext, repositories, migrations |
| 07 | [Inter-Module Communication](./07-inter-module-communication.md) | Domain vs integration events, outbox, sync facade |
| 08 | [Error Handling](./08-error-handling.md) | RESTful ProblemDetails mapping |
| 09 | [Multitenancy](./09-multitenancy.md) | TenantId, ITenantContext, on-demand scoping |
| 10 | [Testing](./10-testing.md) | Opt-in testing: architecture tests (always), E2E + unit/integration on demand |
| 11 | [Cross-Cutting Concerns](./11-cross-cutting.md) | Tracing, idempotency, pagination, versioning, health |

## Naming conventions

- Projects: `Dominodo.<Area>.<Layer>` (e.g. `Dominodo.Pqrs.Application`).
- Commands/queries: `<Verb><Noun>Command` / `Get<Noun>Query`, handlers `<Request>Handler`.
- Integration events: `<Noun><PastTenseVerb>IntegrationEvent` (e.g. `PackageRegisteredIntegrationEvent`).
- Domain events: `<Noun><PastTenseVerb>DomainEvent`.
- Public module facade: `I<Module>ModuleApi` in `Contracts`.
- Tests: `<ClassUnderTest>Tests`, one test class per class under test.
