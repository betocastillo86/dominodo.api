# 10 — Testing

## Testing philosophy — tests are opt-in

The single rule that governs everything here:

> **No test is ever generated automatically.** Asking for a feature, a use case, an endpoint, or any
> code change does **not** produce tests as a side effect. Tests are written **only when you explicitly
> ask for them**, and then **only for the cases you name**.

This is a deliberate reaction to auto-generated tests that restate the implementation, add little
signal, and become a delivery bottleneck. You stay in control of what gets tested and when.

The one standing exception is **architecture tests** — see below. They are the automated form of
Rule #5, written once and maintained as boundaries evolve, not churned per feature.

## The layers

| Layer                       | When it exists                                                        |
| --------------------------- | --------------------------------------------------------------------- |
| **Architecture tests**      | **Always.** Boundary enforcement (Rule #5), gated in CI.              |
| **E2E (black-box HTTP)**    | **On demand.** Standalone suite in `tests/e2e/`; added deliberately.  |
| **Unit / integration**      | **On demand only.** Created *if and when you explicitly request them* for specific cases. Never by default. |

Toolset for architecture tests: **xUnit** (runner) + **NetArchTest**. The E2E suite has its own
independent toolset, documented in full in [`tests/e2e/README.md`](../../tests/e2e/README.md)
(NUnit + Refit + Bogus + Shouldly + Polly). Unit/integration tooling (below) is only wired up the
first time you ask for that kind of test.

## Architecture tests (always on)

Encode the boundary rules from this guide so a violation breaks the build. They live in
`Dominodo.ArchitectureTests` and run in CI as a required check.

```csharp
public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrPersistence()
    {
        var result = Types.InAssembly(typeof(Pqr).Assembly)
            .ShouldNot().HaveDependencyOnAny("Dominodo.Pqrs.Application", "Dominodo.Pqrs.Persistence")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(because: string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Modules_MayOnlyReferenceOtherModulesContracts()
    {
        // e.g. Pqrs must not depend on Tenants' Domain/Application/Persistence — only Tenants.Contracts
        var result = Types.InAssembly(typeof(Pqrs.Application.DependencyInjection).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "Dominodo.Tenants.Domain", "Dominodo.Tenants.Application", "Dominodo.Tenants.Persistence")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Handlers_ShouldBeInternal()
    {
        var result = Types.InAssembly(typeof(Pqrs.Application.DependencyInjection).Assembly)
            .That().ImplementInterface(typeof(IRequestHandler<,>))
            .Should().NotBePublic()
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
```

These are the only tests that are maintained without an explicit request — they are the automated
enforcement of the README's dependency table.

## E2E tests (on demand)

The behavioral test layer. A **standalone .NET solution** under `tests/e2e/` that talks to the running
API only over HTTP, with **hand-written** clients and models (never generated from OpenAPI) so a
contract change surfaces as a loud test failure. It is added **deliberately, in a separate
human-reviewed step** — never as a byproduct of shipping a feature.

Its structure, rules, auth/multitenancy strategy, eventual-consistency handling, and evolution flow
are defined in its own authoritative document — **read it before touching the suite**:

> [`tests/e2e/README.md`](../../tests/e2e/README.md)

## Unit / integration tests (on demand only)

There is **no standing unit or integration test project**. Nothing here is generated with a feature.
When — and only when — you explicitly ask for unit or integration coverage for a specific case, create
what is needed and no more:

- **Unit tests** go in `Dominodo.<Module>.UnitTests` (references the module's `Application`/`Domain`).
  Test one thing; mock only its ports. Aggregates and validators need no mocks.

  ```csharp
  [Fact]
  public void Close_WhenAlreadyClosed_ReturnsConflict()
  {
      var pqr = Pqr.Open(Guid.NewGuid(), Guid.NewGuid(), "Leak", "Water leak").Value;
      var clock = new FixedClock(DateTimeOffset.UtcNow);
      pqr.Close(clock);

      var result = pqr.Close(clock);

      result.IsFailure.Should().BeTrue();
      result.Error.Type.Should().Be(ErrorType.Conflict);
  }
  ```

- **Integration tests** go in `Dominodo.<Module>.IntegrationTests` — the host booted with
  `WebApplicationFactory`, a test database, and external HTTP stubbed with **WireMock**. Reusable stub
  builders and shared helpers live in a `Dominodo.TestUtilities` project, also created on first need.

  ```csharp
  [Fact]
  public async Task Post_EmptySubject_Returns400WithFieldError()
  {
      var response = await Client.PostAsJsonAsync("/api/v1/pqrs",
          new { apartmentId = SeededData.ApartmentId, subject = "", body = "b" });

      response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }
  ```

These projects are created the first time they are requested and wired into the solution then — do not
scaffold them speculatively.

## Do / Don't

- **Do** encode every boundary rule as an architecture test and gate CI on it.
- **Do** write unit or integration tests **when explicitly asked**, covering exactly the cases named.
- **Do** add E2E coverage as a deliberate, separate step (see `tests/e2e/README.md`).
- **Don't** generate any test as a side effect of a feature, prompt, or refactor. If tests were not
  explicitly requested, produce none.
- **Don't** scaffold empty unit/integration test projects "for later" — create them on first real need.
- **Don't** assert on private state; assert on returned `Result`s and observable behavior.
