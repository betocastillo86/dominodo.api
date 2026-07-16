---
name: domi-add-feature-slice
description: Scaffold a CQRS use case end to end in a Dominodo module — the command or query record, its FluentValidation validator, the internal handler returning Result<T>, the thin controller action (with ProblemDetails mapping), and any public DTO in Contracts. Use ONLY in the Dominodo (dominodo.api) repo when the user wants to add a use case, command, query, endpoint, or feature to an existing module. Dominodo-specific — do not use for Keller Postman services.
---

# Add a CQRS feature slice to a Dominodo module

In Dominodo the HTTP endpoint is the trivial part — the real unit of work is the **use case**: a
command (changes state) or a query (reads state), one handler each, dispatched through MediatR. This
skill scaffolds the whole slice so the controller stays a thin adapter over the application layer.

## Before you start — read these

1. `docs/architecture/03-cqrs-mediatr.md` — the command/query/handler pattern (authoritative, with examples).
2. `docs/architecture/04-validation.md` — the validator + ValidationBehavior.
3. `docs/architecture/08-error-handling.md` — how `Result`/`Error` map to HTTP ProblemDetails.
4. **Mirror an existing feature folder** in the same module if one exists — copy its exact shape.

## Inputs to confirm with the user

- Which **module** (must already exist under `src/Modules/`; if not, use `domi-new-module` first).
- **Command or query?** (state change vs read).
- The **use-case name** → `<Verb><Noun>Command` / `Get<Noun>Query`.
- Whether the result shape is consumed by **another module** (if so, the DTO goes in `Contracts`).

## Steps

All application types are `internal`. Colocate the request, validator, and handler in one feature
folder: `Dominodo.<Module>.Application/<Aggregate>/<UseCase>/`.

1. **Request record** — `internal sealed record <Name>Command(...) : ICommand<TResponse>` (or
   `: ICommand` for no payload, or `: IQuery<TResponse>`). Response types: commands typically return
   an id or nothing; queries return a DTO shaped for the caller.

2. **Validator** — `internal sealed class <Name>CommandValidator : AbstractValidator<<Name>Command>`
   with `RuleFor` rules. The `ValidationBehavior` runs it automatically; it short-circuits with a
   validation `Result` (it does not throw).

3. **Handler** — `internal sealed class <Name>CommandHandler(...) : ICommandHandler<<Name>Command, TResponse>`:
   - Constructor-inject the domain-owned repository/port, `ITenantContext`, and any other module's
     `IModuleApi` (from its `Contracts`) needed for a cross-module **read**.
   - Return `Result`/`Result<T>` — expected failures are `Error` values, not exceptions.
   - **Commands:** mutate/add the aggregate via the repository and return. Do **not** call
     `SaveChangesAsync` — the `UnitOfWorkBehavior` owns the transaction and flushes the outbox.
   - **Queries:** project straight into a DTO, read-only, `AsNoTracking()`, scoped with
     `.ForCurrentTenant(tenant)` (see `docs/architecture/09-multitenancy.md`).
   - Cross-module write → publish an integration event from `Contracts` (never dispatch another
     module's request; never touch its schema).

4. **DTO** — if the shape is internal to the module, keep it in `Application`. If another module
   consumes it (via the facade or an event), put it in `Contracts`.

5. **Controller action** — a thin action in the module's controller (hosted by `Dominodo.Api`) that
   binds the request, sends it via MediatR, and maps the `Result` to an HTTP response / ProblemDetails
   using the shared mapping. No business logic in the controller.

6. **Tests — opt-in only.** Do **not** generate tests as part of this slice. Ship the code without any
   unit/integration/E2E tests **unless the user explicitly asked for them** in this request. If they did,
   write only the cases they named, following `docs/architecture/10-testing.md` (unit → `Dominodo.<Module>.UnitTests`,
   integration → `Dominodo.<Module>.IntegrationTests`, both created on first need; E2E → the separate
   `tests/e2e/` suite, see `tests/e2e/README.md`).

## Verify before declaring done

- `dotnet build` succeeds.
- `Dominodo.ArchitectureTests` still passes (no leaked references, everything still `internal`).

## Guardrails

- Request, validator, handler, facade impl stay `internal`.
- Never call `SaveChangesAsync` in a handler.
- Never mutate state in a query handler.
- Cross-module read → `IModuleApi`; cross-module write → integration event; never a direct reference
  to another module's `Domain`/`Application`/`Persistence`.
