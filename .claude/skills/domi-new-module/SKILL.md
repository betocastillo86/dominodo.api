---
name: domi-new-module
description: Scaffold a new bounded-context module in the Dominodo modular monolith — the five projects (Domain, Application, Api, Contracts, Persistence), DbContext + schema, DI wiring, and Contracts skeleton, wired into the host and solution. Use ONLY in the Dominodo (dominodo.api) repo when the user wants to create/add a new module or bounded context. This is Dominodo-specific — do not use for Keller Postman services.
---

# Scaffold a new Dominodo module

Creates a new bounded context as five projects plus tests, wired into the host and enforced by the
architecture tests. Dominodo is a modular monolith — every module is its own miniature clean
architecture that could be extracted into a service later.

## Before you start — read these

1. `docs/architecture/01-modular-monolith.md` — module anatomy and boundaries (authoritative).
2. `docs/architecture/README.md` — the dependency table and solution layout.
3. **If any module already exists** under `src/Modules/`, open it and **mirror it exactly** — its
   project files, references, folder layout, and DI wiring are the source of truth. Copy proven
   structure; do not invent. The docs describe intent; existing code shows the concrete shape.

## Inputs to confirm with the user

- **Module name** in PascalCase (e.g. `Pqrs`, `Billing`). Used in every project name `Dominodo.<Module>.*`.
- **Schema name** in lowercase (e.g. `pqrs`) — the module's own DB schema. No other module touches it.
- Whether the module needs a **public facade** (`I<Module>ModuleApi`) now, or only later.

## Steps

Create everything under `src/Modules/<Module>/`. Match the csproj style, target framework, nullable
and analyzer settings from `Directory.Build.props` / `Directory.Packages.props` (central package
management — never pin versions in the csproj).

1. **`Dominodo.<Module>.Domain`** — references `Shared.Kernel` only. Add an empty feature folder for
   the first aggregate and the domain-owned port interface(s) (e.g. `I<Aggregate>Repository`).

2. **`Dominodo.<Module>.Contracts`** — references `Shared.Kernel` only. Keep it thin. Add:
   - `I<Module>ModuleApi` interface (if a facade is needed) with its public DTOs.
   - An `IntegrationEvents/` folder for `<Noun><PastTenseVerb>IntegrationEvent` records.

3. **`Dominodo.<Module>.Application`** — references own `Domain`, `Shared.Kernel`,
   `Shared.Abstractions`, `Shared.Application`, and other modules' `*.Contracts` as needed. It must
   **NOT** reference `Shared.Infrastructure` or ASP.NET. Everything here is `internal`. Add:
   - A feature folder convention (one folder per use case).
   - The **internal** `I<Module>ModuleApi` implementation that delegates to this module's MediatR.
   - `DependencyInjection.cs` exposing `public static IServiceCollection Add<Module>Module(this IServiceCollection, IConfiguration)`
     that registers MediatR handlers + validators from this assembly (`includeInternalTypes: true`),
     the facade impl, and calls `Add<Module>Persistence(config)`.
   - `<InternalsVisibleTo Include="Dominodo.<Module>.Api" />` in the csproj, so the sibling `*.Api`
     controllers can build the module's `internal` commands/queries without making them public.

4. **`Dominodo.<Module>.Api`** — the module's inbound HTTP adapter: controllers ONLY. References own
   `Application`, `Shared.Infrastructure` (for `ErrorResults.ToProblem`), `Shared.Kernel`; a
   `FrameworkReference` to `Microsoft.AspNetCore.App`; the `MediatR` package (controllers use `ISender`).
   It must **NOT** reference any `*.Persistence`. Add:
   - A marker interface `I<Module>ApiMarker` (used by the host's `AddApplicationPart`).
   - A `Controllers/` folder. Controllers are thin: bind the request, `sender.Send(...)`, map the
     `Result` to HTTP via `ErrorResults.ToProblem`. Namespace `Dominodo.<Module>.Api.Controllers`.
   - Create the project even if the module exposes no HTTP yet (empty but with the marker), to fix the
     pattern — mirror `Dominodo.Admin.Api`.

5. **`Dominodo.<Module>.Persistence`** — references own `Application`, own `Domain`,
   `Shared.Infrastructure`. Add:
   - `<Module>DbContext` mapped to schema `<schema>` (`modelBuilder.HasDefaultSchema("<schema>")`).
   - An `Add<Module>Messaging(this WolverineOptions, connectionString)` helper (called from the host's
     `UseWolverine`) that does `AddDbContextWithWolverineIntegration<<Module>DbContext>(...)` with
     `UseSqlServer` + the `AuditableEntityInterceptor` (NOT a domain-events interceptor — that was
     removed), then `PersistMessagesWithSqlServer(cs, Ancillary).Enroll<<Module>DbContext>()`.
   - `Add<Module>Persistence(this IServiceCollection)` registering repositories and the unit of work as
     `WolverineUnitOfWork<<Module>DbContext>` (over `IDbContextOutbox<<Module>DbContext>`) — this is
     what routes domain events through the durable outbox in the same tx. See `docs/architecture/06`.
   - Repository implementations of the domain-owned ports; `Configurations/` for EF `IEntityTypeConfiguration`.

6. **No test projects by default.** Tests are opt-in (see `docs/architecture/10-testing.md`). Do **not**
   scaffold `Dominodo.<Module>.UnitTests` / `Dominodo.<Module>.IntegrationTests` when creating a module —
   create them only if the user explicitly asks for unit/integration coverage, at that point. Boundaries
   are enforced by `Dominodo.ArchitectureTests`; behavioral coverage lives in the standalone E2E suite
   under `tests/e2e/` (its own solution, added deliberately — see `tests/e2e/README.md`).

7. **Wire into the host** `src/Bootstrap/Dominodo.Api`: call `services.Add<Module>Module(config)` +
   `services.Add<Module>Persistence()`, add `opts.Add<Module>Messaging(cs)` inside `UseWolverine`, and
   register the controllers via `.AddApplicationPart(typeof(Dominodo.<Module>.Api.I<Module>ApiMarker).Assembly)`.
   The host is the ONLY project that references `*.Persistence` and `*.Api`.

8. **Add every new project to `Dominodo.sln`** (including `*.Api`).

## Verify before declaring done

- `dotnet build` succeeds.
- `dotnet test` on `Dominodo.ArchitectureTests` **passes** — this is what enforces the boundaries.
  A green build with red architecture tests means the module is wired wrong (usually a bad project
  reference). Fix references until it passes; do not weaken the tests.

## Guardrails

- Only `Contracts` may be referenced by other modules — never `Domain`/`Application`/`Api`/`Persistence`.
- No cross-module foreign keys, joins, or shared schema.
- Requests/handlers/facade impl are `internal`; controllers live ONLY in `*.Api`.
- `*.Application` must not reference `Shared.Infrastructure` or ASP.NET (its behaviors come from
  `Shared.Application`). `*.Api` must not reference `*.Persistence`.
- The module must not reference concrete adapters (`Adapters.*`) — only the host does.
