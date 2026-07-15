# Dominodo — E2E test suite

This folder holds a **standalone .NET solution** (`Dominodo.E2E.sln`) that tests the Dominodo API
**as a black box, over HTTP**. It lives in the same repository for convenience, but it **shares not a
single line of code with the API**: it references no project under `src/`, reuses none of its DTOs, and
is never generated from its OpenAPI.

This document is the project's **definition and authority**: what it is, how it is structured, which
rules are non-negotiable, and **how it evolves alongside the API without becoming coupled to it**. Read
it before creating a client, a model, a builder, or a test.

---

## 1. The fear this design addresses (and how)

> *"If I introduce a bug in the API, I don't want the E2E tests to 'auto-correct' around the bug and make
> it look like everything works."*

Living in a separate solution does **not** solve that on its own. What solves it is **a discipline**, and
the golden rules below derive from it:

1. **Models and routes are written by hand, never generated from the API's Swagger/OpenAPI.**
   If you generated the client from the API's contract, any contract change — including a defective one —
   would silently propagate into the test and mask the bug. Writing them by hand means the test encodes
   the **expected** contract; if the API drifts, the test **breaks loudly**. That break is the product,
   not a defect.
2. **The suite tests expected behavior (the specification), not the implementation.** A test is written
   from "what should happen", not from "what the handler does today".
3. **Independence is enforced physically:** separate solution, zero `ProjectReference` into `src/`, and a
   CI check that fails the build if someone adds one (see §5).

The code duplication (replicated models) **is deliberate and is the price of isolation.** It is not
technical debt; it is the barrier that stops a bug and its test from moving together.

---

## 2. Non-negotiable rules

1. **Black box over HTTP.** The only contact point with the API is its HTTP surface. No `DbContext`, no
   invoking handlers, no `WebApplicationFactory`. Those are already covered by the integration tests
   inside `src/` (see `docs/architecture/10-testing.md`); this suite is **a different layer**.
2. **Zero code coupling with the API.** No project in this solution references a project under `src/`.
   Models are replicated by hand in `Dominodo.E2E.Clients`.
3. **No codegen from OpenAPI.** Refit interfaces and models are written and maintained by hand.
4. **Clients are used only in the `Act`.** The `Arrange` is built with **RequestBuilders**; the `Assert`
   validates the response. The `Act` is the call to the Refit client under test.
5. **One identity axis and one tenant axis, kept separate.** *Who you are* = JWT (real login, cached).
   *Where you act* = slug in the `X-Tenant` header. They are independent (see §7).
6. **Eventual consistency = explicit wait.** Any assertion that depends on an integration event (a
   cross-module effect) is done via *polling with retries*, never an immediate assert (see §8).

---

## 3. Solution layout

Mirrors the Pollaya model, renamed to Dominodo and aligned to the module vocabulary
(`Users`, `Tenants`, `Operations`, `Admin`).

```
tests/e2e/                                  # E2E solution root — NOT included in Dominodo.sln
  Dominodo.E2E.sln
  Directory.Build.props                     # TFM = the API's (net9.0), nullable, ImplicitUsings
  Directory.Packages.props                  # (optional) Central Package Management

  src/
    Dominodo.E2E.Core/                      # cross-cutting, no HTTP
      Autofixture/                          #   AutoDataAttribute, InlineAutoData
      Faker/                                #   Bogus extensions (E.164 phones, NIT, slugs...)
      Context/                              #   AmbientTenantContext (AsyncLocal: current slug)
      Policies/                             #   RetryPolicies (Polly) for eventual consistency
      DominodoConstants.cs                  #   Headers, Roles, Defaults (default slug)

    Dominodo.E2E.Clients.Core/              # HTTP plumbing (no business logic)
      Api/         ApiSettings.cs           #   BaseUrl, DefaultTenantSlug, timeouts
      Handlers/                             #   chained DelegatingHandlers
        AuthorizationHandler.cs             #     injects Bearer (token from cached real login)
        TenantHeaderHandler.cs              #     injects X-Tenant from AmbientTenantContext
        CorrelationIdHandler.cs             #     injects X-Correlation-Id + X-TestName
        LoggingHandler.cs
        DefaultRetryHandler.cs
      Context/     TestExecutionContext.cs  #   AsyncLocal: correlationId + testName
      Models/                               #   replicated base responses: ProblemDetailsModel,
                                            #     PagedResultModel<T>, CreatedModel

    Dominodo.E2E.Clients/                   # Refit clients + models + builders, PER MODULE
      Common/      BaseRequestBuilder.cs
      Auth/                                 #   IAuthClient + IAuthTokenProvider (real login, cached)
      Modules/
        Users/       IUsersClient.cs  Models/  UsersRequestBuilder.cs
        Tenants/     ITenantsClient.cs Models/  TenantsRequestBuilder.cs
        Operations/  IOperationsClient.cs Models/ OperationsRequestBuilder.cs
        Admin/       IAdminClient.cs  Models/  AdminRequestBuilder.cs
      ClientsServiceRegister.cs             #   AddUsersClient(), AddTenantsClient(), ... + handlers

  tests/
    Dominodo.E2E.Tests.Shared/              # shared base classes + fixture + seeding
      BaseE2ETests.cs                       #   Fixture, Faker, correlation/test-name per test
      E2ESetupFixtureBase.cs                #   OneTimeSetUp: DI + default-tenant seeding
      Seeding/                              #   seeds default tenant, roles, super-admin
    Dominodo.E2E.Tests.Users/               # 1 project per module (SetUpFixture is per-assembly)
    Dominodo.E2E.Tests.Tenants/
    Dominodo.E2E.Tests.Operations/
    Dominodo.E2E.Tests.Admin/
```

**Project dependencies** (all point "toward the core", just as the API points inward):

| Project                      | References                                              |
| ---------------------------- | ------------------------------------------------------- |
| `E2E.Core`                   | NuGet only                                              |
| `E2E.Clients.Core`           | `E2E.Core`                                              |
| `E2E.Clients`                | `E2E.Clients.Core`, `E2E.Core`                          |
| `E2E.Tests.Shared`           | `E2E.Clients`, `E2E.Core`                               |
| `E2E.Tests.<Module>`         | `E2E.Tests.Shared` (and transitively, the above)        |
| **anything → `src/`**        | **forbidden** (enforced in CI, §5)                      |

> **Decision — one test project per module.** This is your explicit requirement and gives better
> isolation and parallelism. Cost to accept: NUnit's `[SetUpFixture]` is **per assembly**, so each module
> project boots its own `ServiceProvider` and runs the seeding. That is why the `SetUpFixture` and the
> seeding live in `E2E.Tests.Shared` as a reusable base, and each module has a one-line `SetUpFixture`
> that inherits from `E2ESetupFixtureBase`.

---

## 4. Client layer (Refit + handlers)

One client per module. A Refit interface with **versioned, hand-written routes**
(`/api/v1/...`, see `docs/architecture/11-cross-cutting.md`). The token is passed as a Refit
`[Authorize("Bearer")]` parameter (null ⇒ anonymous request, to test public endpoints and 401s).

```csharp
public interface IOperationsClient
{
    [Post("/api/v1/requests")]
    Task<ApiResponse<CreatedModel>> CreateRequest(
        [Body] NewRequestModel model,
        [Authorize("Bearer")] string? token = null);

    [Get("/api/v1/requests/{id}")]
    Task<ApiResponse<RequestModel>> GetRequestById(
        Guid id,
        [Authorize("Bearer")] string? token = null);

    [Get("/api/v1/requests")]
    Task<ApiResponse<PagedResultModel<RequestModel>>> GetRequests(
        [Query] RequestFilterModel filter,
        [Authorize("Bearer")] string? token = null);
}
```

**Chained handlers** (registered in `ClientsServiceRegister`, one set per client):

- `TenantHeaderHandler` — injects `X-Tenant: <slug>` from `AmbientTenantContext` (the current test class's
  slug, or the default tenant's). Overridable per call.
- `AuthorizationHandler` — rewrites nothing; the token already comes from `[Authorize("Bearer")]`. Kept as
  an extension point for cross-cutting policies; today it is a passthrough.
- `CorrelationIdHandler` — `X-Correlation-Id` + `X-TestName` from `TestExecutionContext` (end-to-end
  traceability against the API logs, see `docs/architecture/11-cross-cutting.md`).
- `LoggingHandler`, `DefaultRetryHandler` — structured logging and transport retries (5xx/timeout),
  **not** assertion retries.

**Serialization:** `System.Text.Json` aligned to the API's ASP.NET Core defaults (enums as strings,
`DateTimeOffset` ISO-8601, camelCase). We do *not* use Newtonsoft (Pollaya did, for its legacy API;
Dominodo is greenfield and must match what the host emits).

---

## 5. Independence, enforced (not just promised)

Three barriers, cheapest to strongest:

1. **Separate solution.** `Dominodo.E2E.sln` never includes projects from `src/`, and `Dominodo.sln`
   (the API's) never includes `tests/e2e/`.
2. **CI guard.** A check that fails the build if a `ProjectReference` leaves `tests/e2e/` toward `src/`:

   ```bash
   # scripts/e2e-guard.sh — runs in the E2E workflow
   if grep -rl --include="*.csproj" -E 'ProjectReference[^>]*\.\./\.\./src/' tests/e2e; then
     echo "❌ The E2E suite cannot reference API projects (src/)."; exit 1
   fi
   ```
3. **No codegen.** There is no MSBuild target or script that generates clients from the OpenAPI. If it is
   ever wanted, it is discussed as an architecture change — because it **breaks the core property** of
   this suite.

---

## 6. Hand-written replicated models

They live in `Modules/<Module>/Models/`, one per request/response. They mirror the API's public DTO
(`*.Contracts`) **by value, not by reference**: same field names, equivalent types.

```csharp
// Dominodo.E2E.Clients/Modules/Operations/Models/NewRequestModel.cs
public sealed class NewRequestModel
{
    public Guid ApartmentId { get; init; }
    public string Type { get; init; } = default!;   // "Peticion" | "Queja" | ...
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string? Location { get; init; }
}

// RequestModel, PagedResultModel<T>, ProblemDetailsModel (RFC 9457), CreatedModel...
```

Convention: `Model` suffix (not `Dto`, to avoid confusion with the API's DTOs). `New*` for creation,
`Update*` for edits, `*FilterModel` for query strings.

---

## 7. Authentication and multitenancy in E2E

Reflects `docs/architecture/09-multitenancy.md`: **the `X-Tenant` header slug decides the tenant; the JWT
only validates** that the user belongs to that tenant.

### Identity — cached real login

An `IAuthTokenProvider` that calls the real auth endpoints and **caches by `(user, tenantSlug)`** to avoid
re-logging in on every test. It exercises the real auth flow; the cost is that an auth bug takes down many
suites (acceptable and desirable: auth is critical).

```csharp
public interface IAuthTokenProvider
{
    // real login + tenant selection; cached
    Task<string> GetTokenAsync(string phone, string password, string tenantSlug);
    Task<string> GetTokenForRoleAsync(string role, string tenantSlug); // seeded users per role
}
```

### Tenant — ambient slug + on-demand creation

`AmbientTenantContext` (AsyncLocal) holds the "current" slug; `TenantHeaderHandler` injects it on every
request. The chosen data strategy (**seeded default tenant + new tenants when the case needs one**,
Pollaya-style):

- The `SetUpFixture` seeds **one default tenant** (`DominodoConstants.Defaults.TenantSlug`,
  e.g. `e2e-default`) with its roles, a super-admin, and base users per role. Most tests act on it.
- When a case needs strong isolation or virgin data, `TenantsRequestBuilder.CreateTenant()` creates a new
  one (as super-admin), returns its slug, and the test sets it on the `AmbientTenantContext` for its calls.

> **Risk to watch (shared tenant):** tests that write to the default tenant can contaminate each other
> (e.g. counts, paginated listings). Mitigation: assertions that do not depend on global state (filter by
> the resource created in the test, not by tenant totals), and move any case sensitive to data volume to
> its own tenant.

### The reconciliation matrix as a first-class test

Exactly the kind of bug a coupled test would mask. Cover the doc-09 table explicitly:

| Case                                    | `X-Tenant` | Token           | Expected             |
| --------------------------------------- | ---------- | --------------- | -------------------- |
| Regular user, own site                  | slug of A  | JWT of A        | `200`                |
| Regular user, another tenant's slug     | slug of B  | JWT of A        | `403 Tenant.Mismatch`|
| Unknown slug                            | `nope`     | any             | `400 Tenant.Unknown` |
| Anonymous on a public endpoint          | slug of A  | —               | `200`                |
| Tenant user with no header              | —          | JWT of A        | `403 Tenant.Mismatch`|
| Super-admin cross-tenant                | absent     | super-admin JWT | `200` (all)          |

---

## 8. Test layer

**Stack:** NUnit + AutoFixture + Bogus + Shouldly + Polly (same libraries as Pollaya).

- `BaseE2ETests` (in `Shared`): exposes `Fixture`, `Faker`, sets `TestExecutionContext` (correlationId +
  test name) in `[SetUp]` and clears it in `[TearDown]`.
- Each module has a `Base<Module>Tests` that resolves its builders and clients from the `ServiceProvider`
  in `[OneTimeSetUp]`, and sets the `AmbientTenantContext` to the default tenant (or creates its own).
- **Test structure:** `Arrange` with builders → `Act` = **one** call to the Refit client under test →
  `Assert` on `StatusCode`, `Content`, and/or the `ProblemDetailsModel`.

```csharp
[TestFixture]
public class CreateRequestTests : BaseOperationsTests
{
    [Test]
    public async Task _401_WhenNoToken()
    {
        // Act — no token (anonymous)
        var response = await OperationsClient.CreateRequest(
            OperationsRequestBuilder.BuildNewRequestModel());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task _201_CreatesRequest_ForResident()
    {
        // Arrange — a resident with an apartment in the current tenant
        var (token, resident, apartment) =
            await OperationsRequestBuilder.SetupResidentWithApartment();
        var model = OperationsRequestBuilder.BuildNewRequestModel(apartmentId: apartment.Id);

        // Act
        var response = await OperationsClient.CreateRequest(model, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Content!.Id.ShouldNotBe(Guid.Empty);
    }
}
```

### RequestBuilders — the heart of the `Arrange`

One builder per module, composable: a builder may depend on others to assemble complete use cases (just
like Pollaya's `TriviasRequestBuilder` depends on `OrdersRequestBuilder`/`LeaguesRequestBuilder`). In
Dominodo this models the domain's natural dependency: to create a PQR you need tenant + user + membership
+ apartment.

```csharp
public sealed class OperationsRequestBuilder : BaseRequestBuilder
{
    private readonly IOperationsClient _operations;
    private readonly TenantsRequestBuilder _tenants;   // creates tenant/apartment
    private readonly UsersRequestBuilder _users;       // creates user + membership/role
    // ...ctor injects everything...

    // Builds the model (fake data by default, overridable) — does NOT call the API
    public NewRequestModel BuildNewRequestModel(Guid? apartmentId = null, string? title = null) => new()
    {
        ApartmentId = apartmentId ?? Guid.NewGuid(),
        Type = "Peticion",
        Title = title ?? Faker.Lorem.Sentence(4),
        Description = Faker.Lorem.Paragraph(),
    };

    // Full Arrange use case — DOES call the API (setup, not the Act under test)
    public async Task<(string Token, ResidentModel Resident, ApartmentModel Apartment)>
        SetupResidentWithApartment()
    {
        var apartment = await _tenants.CreateApartment();
        var (token, resident) = await _users.CreateResidentWithMembership(apartment.Id);
        return (token, resident, apartment);
    }
}
```

> **Rule:** builders throw if an `Arrange` step fails (non-success response). A broken `Arrange` must
> **abort** the test, not produce a misleading `Assert`. The only place we evaluate status codes as part
> of the outcome is the `Act`.

### Eventual consistency (integration events)

Cross-module effects are asynchronous (see `docs/architecture/07-inter-module-communication.md`): creating
a `Request` publishes `RequestOpenedIntegrationEvent`, and the `Admin` module consumes it and generates
notifications. An immediate `Assert` would be *flaky*. Use **polling with retries** (Polly), like
Pollaya's `RetryPolicies.CreateAssertionRetryPolicy`:

```csharp
// Act
await OperationsClient.CreateRequest(model, token);

// Eventual assert — retries until the notification appears (or the timeout expires)
await RetryPolicies.Until<PagedResultModel<NotificationModel>>(
    action: () => AdminClient.GetMyNotifications(residentToken),
    predicate: page => page.Items.Any(n => n.Type == "RequestOpened"));
```

---

## 9. Environment and execution

- **What it runs against:** the API + Postgres + bus brought up with **local docker-compose** (or Aspire).
  Tests point at `http://localhost:<port>`. `BaseUrl` and `DefaultTenantSlug` in `appsettings.json`
  (+ optional `appsettings.Local.json`, gitignored) per test project.
- **DB state:** being local and controlled, the `SetUpFixture` may reset/migrate the DB before seeding.
  The primary isolation comes from the **tenant** (default + on-demand), not from the reset.
- **CI:** a dedicated workflow (separate from the API's) that: brings up docker-compose → waits for health
  (`/health/ready`, see doc 11) → runs `dotnet test Dominodo.E2E.sln` → publishes results.
- **Load tests:** **out of scope** for now (deliberately omitted).

---

## 10. How it evolves alongside the API (the critical point)

The E2E suite **follows** the API, on purpose and by intent. The flow:

1. The API adds/changes an endpoint (feature slice; see `docs/architecture/03-cqrs-mediatr.md` and the
   `domi-add-feature-slice` skill).
2. In a **separate, human-reviewed step/PR**, the E2E suite adds:
   - the hand-replicated **model(s)** in `Modules/<Module>/Models/`,
   - the **Refit method(s)** in `I<Module>Client`,
   - the **RequestBuilder** method(s) for the `Arrange`,
   - the **tests** (happy path + errors: 400/401/403/404/409/422 per doc 08, and the doc-09 tenant matrix
     when applicable).
3. **Drift is visible, not silent.** If the API changed a contract, the hand-written model no longer
   matches and the test fails. That failure is the "something changed, review it carefully" signal —
   exactly what you wanted.

**Rules that preserve the property:**

- The API behavior change and the E2E test adjustment **do not go in the same commit without review.** If
  an E2E test must change, the PR must justify *why* the expected contract changed (not "to make it pass").
- **Versioning:** when the API breaks a contract, it bumps version (`/api/v2/...`). The suite keeps the
  `v1` tests while `v1` exists, and adds the `v2` ones. A test is never silently "moved" from v1 to v2.
- **Per-new-feature-slice checklist** (paste into the E2E PR):
  - [ ] Models replicated by hand (not copied from the API project).
  - [ ] Refit method(s) with versioned route.
  - [ ] RequestBuilder for the `Arrange`.
  - [ ] Happy path + relevant errors (doc 08).
  - [ ] Tenant matrix if the endpoint is tenant-scoped/anonymous/super-admin (doc 09).
  - [ ] Eventual assert (Polly) if it fires integration events (doc 07).
  - [ ] Client used **only** in the `Act`.

---

## 11. Naming conventions

- **Projects:** `Dominodo.E2E.<Area>` / `Dominodo.E2E.Tests.<Module>`.
- **Clients:** `I<Module>Client` (`IOperationsClient`).
- **Builders:** `<Module>RequestBuilder`.
- **Models:** `New<Noun>Model`, `Update<Noun>Model`, `<Noun>Model`, `<Noun>FilterModel`.
- **Test classes:** `<Verb><Noun>Tests` (`CreateRequestTests`), one per use case/endpoint.
- **Tests:** `_<StatusCode>_<Scenario>` (`_403_WhenTenantMismatch`) — Pollaya-style, readable in the runner.

---

## 12. Libraries (align versions when creating the `.csproj`)

| Purpose                   | Package                                                        |
| ------------------------- | ------------------------------------------------------------- |
| Test runner               | `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`        |
| Typed HTTP clients        | `Refit`, `Refit.HttpClientFactory`                            |
| Fake data                 | `Bogus`, `AutoFixture`, `AutoFixture.NUnit3`                   |
| Assertions                | `Shouldly`                                                    |
| Retries / eventual        | `Polly`                                                       |
| Config + DI + logging     | `Microsoft.Extensions.*`, `Serilog.Sinks.Console`             |

> Serialization with `System.Text.Json` (not Newtonsoft) to match the Dominodo host.

---

## 13. Status and next steps

This document **is the seed.** The `.csproj` files do not exist yet. To start the implementation:

1. Create `Dominodo.E2E.sln` and the projects from §3 with the API's TFM.
2. Implement `E2E.Clients.Core` (handlers) and `E2E.Core` (auth token provider, contexts, retry).
3. Write the first module end-to-end by hand (`Users` or `Tenants`) as the **canonical exemplar** — the
   one everything else copies — including its seeding and the tenant matrix.
4. Add the CI guard (§5) and the docker-compose workflow (§9).
5. From there, each API feature slice drags along its E2E slice (§10).
