---
name: domi-new-module
description: Scaffold a new bounded-context module in the Dominodo modular monolith — the four projects (Domain, Application, Contracts, Persistence), their test projects, DbContext + schema, DI wiring, and Contracts skeleton, wired into the host and solution. Use ONLY in the Dominodo (dominodo.api) repo when the user wants to create/add a new module or bounded context. This is Dominodo-specific — do not use for Keller Postman services.
---

# Scaffold a new Dominodo module

Creates a new bounded context as four projects plus tests, wired into the host and enforced by the
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
   `Shared.Abstractions`, and other modules' `*.Contracts` as needed. Everything here is `internal`. Add:
   - A feature folder convention (one folder per use case).
   - The **internal** `I<Module>ModuleApi` implementation that delegates to this module's MediatR.
   - `DependencyInjection.cs` exposing `public static IServiceCollection Add<Module>Module(this IServiceCollection, IConfiguration)`
     that registers MediatR handlers + validators from this assembly (`includeInternalTypes: true`),
     the facade impl, and calls `Add<Module>Persistence(config)`.

4. **`Dominodo.<Module>.Persistence`** — references own `Application`, own `Domain`,
   `Shared.Infrastructure`. Add:
   - `<Module>DbContext` mapped to schema `<schema>` (`modelBuilder.HasDefaultSchema("<schema>")`),
     built on the shared base DbContext (interceptors, outbox — see `docs/architecture/06-persistence.md`).
   - `Add<Module>Persistence(this IServiceCollection, IConfiguration)` registering the DbContext and repositories.
   - Repository implementations of the domain-owned ports; `EntityConfigurations/` for EF `IEntityTypeConfiguration`.

5. **Test projects** under `tests/`:
   - `Dominodo.<Module>.UnitTests` (references `Application`, `Domain`, `TestUtilities`).
   - `Dominodo.<Module>.IntegrationTests` (WebApplicationFactory + WireMock — see `docs/architecture/10-testing.md`).

6. **Wire into the host** `src/Bootstrap/Dominodo.Api`: call `services.Add<Module>Module(config)` in
   composition. The host is the ONLY project that references `*.Persistence`.

7. **Add every new project to `Dominodo.sln`.**

## Verify before declaring done

- `dotnet build` succeeds.
- `dotnet test` on `Dominodo.ArchitectureTests` **passes** — this is what enforces the boundaries.
  A green build with red architecture tests means the module is wired wrong (usually a bad project
  reference). Fix references until it passes; do not weaken the tests.

## Guardrails

- Only `Contracts` may be referenced by other modules — never `Domain`/`Application`/`Persistence`.
- No cross-module foreign keys, joins, or shared schema.
- Requests/handlers/facade impl are `internal`.
- The module must not reference concrete adapters (`Adapters.*`) — only the host does.
