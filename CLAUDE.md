# Dominodo — Backend

Dominodo is a **modular monolith** in .NET: one deployable, internally split into isolated
**modules** (bounded contexts), each built so it can later be extracted into its own service with
minimal change.

## The five rules everything derives from

1. **A module owns everything inside it and exposes almost nothing.** The only project another
   module may reference is a module's `Contracts`. Never its `Domain`, `Application`, or `Persistence`.
2. **Modules never share a transaction or a database schema.** Each module owns its schema and its
   own `DbContext`. No foreign keys across module boundaries.
3. **Cross-module writes are asynchronous** (integration events over the bus). **Cross-module reads
   are synchronous** (a module's public `IModuleApi`, called in-process).
4. **Dependencies point inward.** `Domain` depends on nothing; `Application` depends on `Domain`;
   adapters (`Persistence`, `Adapters.*`) depend on the abstractions they implement — never the reverse.
5. **Boundaries are enforced by tests, not convention.** `Dominodo.ArchitectureTests` fails the
   build when a rule above is violated. Run it before considering any structural change done.

## Module anatomy — five projects

```
Modules/<Module>/
  Dominodo.<Module>.Domain        # aggregates, value objects, domain events, domain-owned ports
  Dominodo.<Module>.Application   # CQRS commands/queries + handlers, validators, ports,
                                  #   INTERNAL IModuleApi impl. Everything here is `internal`.
                                  #   NO ASP.NET, NO Shared.Infrastructure.
  Dominodo.<Module>.Api           # the module's inbound HTTP adapter: controllers ONLY. Dispatches the
                                  #   Application's internal commands via ISender (granted by InternalsVisibleTo).
  Dominodo.<Module>.Contracts     # PUBLIC surface ONLY: integration events + IModuleApi + public DTOs
  Dominodo.<Module>.Persistence   # the module's own adapter: DbContext (own schema), repos, EF config, migrations
```

## Dependency rules (enforced by architecture tests)

| Project                 | May reference                                                                      |
| ----------------------- | ---------------------------------------------------------------------------------- |
| `*.Domain`              | `Shared.Kernel` only                                                               |
| `*.Application`         | own `Domain`, `Shared.Kernel`, `Shared.Abstractions`, `Shared.Application`, other modules' `*.Contracts`. **NOT** `Shared.Infrastructure`, **NOT** ASP.NET |
| `*.Api`                 | own `Application`, `Shared.Infrastructure`, `Shared.Kernel`; `FrameworkReference` to `Microsoft.AspNetCore.App`. The only module project with controllers |
| `*.Contracts`           | `Shared.Kernel` (keep thin)                                                        |
| `*.Persistence`         | own `Application`, own `Domain`, `Shared.Infrastructure`                           |
| `Adapters.*`            | `Shared.Abstractions`, `Shared.Kernel`                                             |
| `Shared.Application`    | `Shared.Kernel` (+ MediatR, FluentValidation, Logging.Abstractions). Application-layer plumbing (validation / UoW / logging behaviors). **NOT** infra, **NOT** ASP.NET |
| `Shared.Infrastructure` | `Shared.Kernel`, `Shared.Abstractions`                                             |
| `Dominodo.Api` (host)   | everything (composition root only — the ONLY project referencing `Adapters.*` and `*.Persistence`) |

Also: MediatR requests/handlers are `internal` — a module cannot dispatch another module's requests.

**Controllers live in the module's `*.Api` project**, registered with the host via `AddApplicationPart`.
`*.Api` references `Shared.Infrastructure` (for the HTTP helpers its controllers use, e.g.
`ErrorResults.ToProblem`) and takes a `FrameworkReference` to `Microsoft.AspNetCore.App`. It dispatches
the module's `internal` MediatR commands via `ISender` — access granted by an `InternalsVisibleTo` on
`*.Application`, so commands/queries stay `internal`. `*.Application` itself references **neither**
`Shared.Infrastructure` **nor** ASP.NET; its application-layer plumbing (the validation / UoW / logging
MediatR behaviors) lives in `Shared.Application`.

## Architecture reference — read the doc BEFORE working in that area

The detailed patterns live in `docs/architecture/`. They are **not** loaded automatically — open the
relevant one on demand before touching that concern. Do not guess a pattern; consult its doc.

| When you're about to…                          | Read first                                          |
| ----------------------------------------------- | --------------------------------------------------- |
| Create a new module / reason about boundaries   | `docs/architecture/01-modular-monolith.md`          |
| Add an aggregate, value object, or domain event | `docs/architecture/02-ddd-building-blocks.md`       |
| Add a command / query / handler                 | `docs/architecture/03-cqrs-mediatr.md`              |
| Validate request data                           | `docs/architecture/04-validation.md`                |
| Call an external system (email, WhatsApp, …)    | `docs/architecture/05-ports-and-adapters.md`        |
| Persist an aggregate (DbContext, repo, migration) | `docs/architecture/06-persistence.md`             |
| Communicate between modules (event or facade)   | `docs/architecture/07-inter-module-communication.md`|
| Map errors to HTTP responses                    | `docs/architecture/08-error-handling.md`            |
| Anything touching TenantId / scoping            | `docs/architecture/09-multitenancy.md`              |
| Write tests (only when explicitly asked)        | `docs/architecture/10-testing.md`                   |
| Tracing, idempotency, pagination, versioning    | `docs/architecture/11-cross-cutting.md`             |
| Protect an endpoint by permission (not role)    | `docs/architecture/12-permission-authorization.md`  |

The full index and solution map is in `docs/architecture/README.md`.

## Non-negotiables when writing code

- Commands, queries, handlers, and the facade implementation are `internal`.
- Handlers return `Result` / `Result<T>` — expected failures are values, not exceptions.
- Handlers do **not** call `SaveChangesAsync`; the `UnitOfWorkBehavior` owns the transaction. The
  unit of work routes through the module's Wolverine outbox, so any domain events raised by the
  saved aggregates are persisted transactionally and delivered async/durable — not in-process.
- Domain events are **not** MediatR notifications. They go to the module's durable outbox (same tx as
  the aggregate) and are handled async by in-module **Wolverine** handlers. See `docs/architecture/07`.
- Cross-module read → call the other module's `IModuleApi` from its `Contracts`. Cross-module write
  → publish an integration event. Never reference another module's non-`Contracts` project.
- One command/query + validator + handler per use case, colocated in a feature folder.
- **Tests are opt-in.** Never generate tests as a side effect of a feature, prompt, or refactor. Write
  unit/integration/E2E tests **only when explicitly asked**, and only for the cases named. Architecture
  tests (`Dominodo.ArchitectureTests`) are the sole exception — always maintained. See `docs/architecture/10-testing.md`.

## Code style

- **Braces are mandatory on every control block** — `if`, `else`, `for`, `foreach`, `while`, `do`,
  `using`, `lock`, `fixed` — even when the body is a single line. No braceless one-liners. Enforced by
  `.editorconfig` (`csharp_prefer_braces = true` / `IDE0011`) with `EnforceCodeStyleInBuild` +
  `TreatWarningsAsErrors`, so a violation **breaks the build**. Run `dotnet format analyzers --diagnostics IDE0011`
  to fix mechanically.
- **Comment sparingly.** Add a comment only for non-obvious *why* — complicated logic, a workaround, a
  subtle invariant.

## Naming conventions

- Projects: `Dominodo.<Area>.<Layer>` (e.g. `Dominodo.Pqrs.Application`).
- Commands/queries: `<Verb><Noun>Command` / `Get<Noun>Query`; handlers `<Request>Handler`.
- Integration events: `<Noun><PastTenseVerb>IntegrationEvent`; domain events `<Noun><PastTenseVerb>DomainEvent`.
- Module facade: `I<Module>ModuleApi` in `Contracts`.
- Tests: `<ClassUnderTest>Tests`, one test class per class under test.
