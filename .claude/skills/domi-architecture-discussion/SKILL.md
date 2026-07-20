---
name: domi-architecture-discussion
description: Think through a software-design problem in the Dominodo modular monolith — new modules, bounded-context sizing, cross-module communication, aggregate boundaries, or a refactoring plan — by researching the codebase and architecture docs, asking clarifying questions, and proposing options with trade-offs. Use ONLY in the Dominodo (dominodo.api) repo when the user wants to DISCUSS and DECIDE a design, NOT implement it. This is the thinking/design phase; hand off to domi-new-module / domi-add-feature-slice for the code. Dominodo-specific.
---

# Discuss & design a Dominodo architecture change

This skill is a **design partner**, not a code generator. You take a problem — "where should this new
capability live?", "is this one module or two?", "how do these two modules talk?", "how do I refactor
X without breaking boundaries?" — and you help reason it to a **decision** through research, questions,
and options with trade-offs.

**Your output is a proposal and (when a real decision is reached) a draft ADR — never implementation
code.** If the user wants the code, that is `domi-new-module` or `domi-add-feature-slice`; say so and
stop. Do not scaffold projects, write handlers, edit `.csproj`, or run `dotnet`. Sketching a class
signature or a folder layout *inside the proposal to illustrate a trade-off* is fine; producing
working code is out of scope.

Because this is Dominodo, "good design" is not generic — it means a design that holds up against
**the five rules** and the **dependency table**. Every option you propose must be evaluated against
them explicitly.

## Operating stance

- **Research before you opine.** Never propose from memory of "how modular monoliths usually work."
  Read the relevant architecture docs, the ADRs, the domain model, and the actual modules first. This
  codebase has strong, specific opinions already written down — honor them or argue explicitly against
  them.
- **Ask, don't assume.** The hardest part of an architecture problem is the requirements you can't see.
  Use `AskUserQuestion` to resolve genuine forks (see Phase 1). Do not invent constraints; do not pick
  a bounded context for the user without confirming.
- **Always more than one option.** Present 2–3 real, viable options with honest trade-offs, then a
  recommendation. A single "here's the answer" is a failure mode — the point of the discussion is to
  make the trade-offs visible.
- **Converge to a decision.** The discussion is not done when options exist; it's done when the user
  has chosen and the *why* is captured. That artifact is a draft ADR.
- **Discuss and write artifacts in Spanish.** The ADRs and domain model (`docs/adr/`, `docs/domain/`)
  are in Spanish; match them. (This SKILL.md is in English to match the other `domi-*` skills.)

## Phase 1 — Frame the problem (research + clarify)

Before proposing anything, build a grounded picture:

1. **Read the architecture docs relevant to the problem.** Do not read all twelve; pick by concern:
   - Boundaries / new module / "one module or two" → `docs/architecture/01-modular-monolith.md`
   - Aggregate, value object, invariant, domain event → `docs/architecture/02-ddd-building-blocks.md`
   - A use case / command / query shape → `docs/architecture/03-cqrs-mediatr.md`
   - Modules talking to each other → `docs/architecture/07-inter-module-communication.md`
   - Persistence / schema / migration impact → `docs/architecture/06-persistence.md`
   - Ports to external systems → `docs/architecture/05-ports-and-adapters.md`
   - TenantId / scoping → `docs/architecture/09-multitenancy.md`
   - Permissions on an endpoint → `docs/architecture/12-permission-authorization.md`
   - The dependency table & solution map → `docs/architecture/README.md`
   The CLAUDE.md "When you're about to…" table is the full routing map.
2. **Read the decision history.** Skim `docs/adr/README.md` (the index) and open any ADR that touches
   the area — a proposal that contradicts an `Accepted` ADR must say so and either supersede it or bend
   to it. Read `docs/domain/00-domain-model.md` for the current, canonical domain shape.
3. **Read the real code.** Look at the existing modules under `src/Modules/` (currently `Admin`,
   `Tenants`, `Users`) and the shared projects under `src/Shared/`. The docs describe intent; the code
   shows the concrete, proven shape. Mirror what exists before inventing.
4. **Restate the problem back to the user** in one or two sentences and name the forces at play
   (competing invariants, consistency needs, who reads/writes what, tenancy). Confirm you have it right.
5. **Ask the clarifying questions that actually change the design.** Use `AskUserQuestion`. Good forks
   for this codebase:
   - Bounded context: is this a new capability in an **existing** module or a **new** module? What
     language do domain experts use — does it map to an existing context or a new one?
   - Ownership: which module **owns** this data (writes it)? Who only **reads** it?
   - Consistency: does the cross-module effect need to be **immediate** (→ synchronous read via
     `IModuleApi`) or is **eventual** acceptable (→ integration event)?
   - Lifecycle & tenancy: platform-scoped or tenant-scoped data? Single aggregate per transaction?
   - Scale of change (for refactors): behavior-preserving move, or a change in the model itself?
   Only ask what you cannot answer from the code/docs. Don't quiz the user on things you can read.

## Phase 2 — Explore the design space against the five rules

Frame every candidate design as a set of choices on the axes this architecture cares about. This is
where the real thinking happens — walk the tensions, don't just pick:

- **Module boundary.** New bounded context vs. a slice inside an existing module. A new module is right
  when it has its own language, its own lifecycle, and could plausibly be a separate service; a slice is
  right when it's the same context gaining a use case. Cite ADR-0004/0005 as examples of how contexts
  were grouped here.
- **Data ownership & the no-shared-schema rule.** Each module owns its schema; **no foreign keys across
  module boundaries**. If design B needs a relationship spanning two modules, that relationship is an
  **id reference + a read through the other module's `IModuleApi`**, or a **copy kept in sync via an
  integration event** — never an FK. Make this explicit; it's the most common place a naive design breaks.
- **Cross-module communication.** Reads are synchronous (`IModuleApi` from `Contracts`); writes are
  asynchronous (integration events). If option X requires module A to *change* module B's state
  synchronously and transactionally, that's a red flag — surface it and redesign.
- **Domain event vs integration event.** In-module reaction, same tx → domain event via the outbox,
  handled by an in-module Wolverine handler. Cross-module notification → integration event on `Contracts`.
- **Aggregate & transaction boundary.** One aggregate is the consistency boundary per transaction. If a
  use case must atomically change two aggregates, question whether they're really one aggregate, or
  whether the second change should be eventual.
- **Where the shape lives.** Internal to the module → `Application`/`Domain`. Consumed by another module
  → `Contracts` (kept thin). Watch for `Contracts` bloat — a fat `Contracts` is a leaking boundary.

For each option, state plainly which rules it satisfies cleanly, which it strains, and what it costs.

## Phase 3 — Present options with trade-offs

Lay out 2–3 options. For each, use this shape:

```
### Opción N — <nombre corto>
**Idea:** una o dos frases.
**Cómo encaja:** módulo(s) afectados, quién posee el dato, cómo se comunican (IModuleApi / evento),
  dónde vive cada tipo (Domain / Application / Contracts / Persistence).
**Cumple:** qué reglas respeta limpiamente.
**Tensiona / cede:** qué regla estira, qué complejidad o deuda añade, qué ADR toca.
**Coste:** esfuerzo relativo, migraciones necesarias, riesgo de romper boundaries.
```

Then a **Recomendación** section: which option and *why*, in terms of the trade-offs — not just
assertion. If the recommendation depends on an answer you don't have, say what would change your mind.

Keep any code sketches to illustrative signatures/folder trees. No working implementations.

## Phase 4 — Capture the decision (draft ADR)

Once the user chooses, the discussion produced a decision — record it so it isn't lost. Offer to draft
an ADR following the repo's format (`docs/adr/README.md`):

- Filename `NNNN-titulo-en-kebab-case.md`, next incremental 4-digit number after the highest in `docs/adr/`.
- **Estado: Proposed** (the user accepts it later; you don't self-accept).
- Sections **Contexto** (problem/forces) · **Decisión** (what we chose) · **Consecuencias** (what we
  gain and what we cede). Short and concrete. In Spanish.
- If it changes a canonical domain fact, note that `docs/domain/00-domain-model.md` will need updating
  and, if it overturns an `Accepted` ADR, that the old one gets marked `Superseded by ADR-NNNN`.
- Add the row to the ADR index table in `docs/adr/README.md`.

Writing this ADR draft is the one file this skill produces — it's a decision record, not code.

## Handoff

End by pointing at the implementation path, explicitly out of scope here:

- New bounded context → **`domi-new-module`**.
- A use case / endpoint inside a module → **`domi-add-feature-slice`**.
- Say plainly: "El diseño está decidido; la implementación va por `<skill>` cuando quieras."

## Guardrails

- **No implementation.** No scaffolding, no handlers/controllers/EF config, no `.csproj` edits, no
  `dotnet build/test/format`. Illustrative signatures inside the proposal only.
- **Never propose a design that breaks the five rules silently.** If the best option strains a rule,
  say so out loud and treat it as a deliberate, ADR-worthy trade-off — not a hidden one.
- **No cross-module FKs, no shared schema, no synchronous cross-module writes** in any recommended
  option. Reads → `IModuleApi`; writes → integration events.
- **Ground every claim in a doc, ADR, or real module** — cite it. If you're guessing, mark it a
  question, not a fact.
- **Always ≥2 options + a reasoned recommendation.** Never a lone answer.
- **Discussion and ADR in Spanish**, matching `docs/adr/` and `docs/domain/`.
