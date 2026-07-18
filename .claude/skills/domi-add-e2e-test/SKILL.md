---
name: domi-add-e2e-test
description: Write black-box HTTP E2E tests for a Dominodo API endpoint (hand-written Refit client, models, RequestBuilder, NUnit tests). Use in the dominodo.api repo when asked to add E2E coverage for an endpoint.
---

# Add an E2E test to the Dominodo suite

The E2E suite (`tests/e2e/Dominodo.E2E.sln`) tests the API **as a black box over HTTP**, sharing **zero
code with `src/`**. Models and routes are **written by hand** so that if the API drifts, the test
**breaks loudly** — that break is the product. This skill is the operational recipe.

## Rule 0 — NEVER touch `src/`

Write only under `tests/e2e/`. Never add or edit API code to help a test — no endpoints, handlers, or
behavior changes. State no production endpoint can create is arranged via the dev-only SQL endpoint
(`POST /api/v1/dev/sql`, client `ISqlClient`, 404 outside Development); keep the SQL in a RequestBuilder
(see `UsersRequestBuilder.ForceActivateUserAsync`). Can't reach the case even so? Stop and say so.

## How the user asks

An endpoint + a list of `status → scenario` cases (e.g. *"get all roles: 401, 403 sin permiso manage
roles, 400 invalid pagination, 200 verificar roles"*). Each bullet becomes one `[Test]` named
`_<status>_<scenario>`.

## Step 1 — Discover the truth from `src/` (never from Swagger, never guess)

Open the real controller `src/Modules/<Module>/Dominodo.<Module>.Api/Controllers/<Name>Controller.cs`
and its Application/Contracts DTOs. Extract, per endpoint:

- **Route + verb + version** → `/api/v1/...` (routes read `api/v{version:apiVersion}`; default = v1).
- **Auth**: `[Authorize]` = any valid bearer (else 401); `[Authorize(Policy = "SuperAdmin")]` or a
  permission requirement = 403 without it. Anonymous endpoints take no token.
- **Query/body shape** → the fields + types to hand-replicate as a `*Model`.
- **Success shape** → `PagedResult<Dto>` (→ `PagedResultModel<T>`), a `Dto`, a created id, or 204.
- **Error codes** → the `Error` codes the handler returns (e.g. `Validation.Failed`, `Role.NotFound`).
  The API's `title` in the RFC 9457 body **is** the error code.
- **Validation rules** → open the request's FluentValidation validator
  (`src/Modules/<Module>/Dominodo.<Module>.Application/<Feature>/<Command>Validator.cs`) and **enumerate
  every `RuleFor`**. This is the source of truth for the `400 Validation.Failed` cases — see Step 6a.
  A `400` bullet is **never** "check one field"; it is "cover the whole validator."

> If a case isn't reachable yet (a permission not wired, or tenancy — see README §7), say so and adjust
> rather than writing a test that can't pass.

## Step 2 — Locate / create the module test project and controller subfolder

One test project per module: `tests/e2e/tests/Dominodo.E2E.Tests.<Module>`. Users exists as the
**exemplar — copy its shape**. The client, models and builder live in
`tests/e2e/src/Dominodo.E2E.Clients/Modules/<Module>/`. If the module project doesn't exist yet, mirror
`Dominodo.E2E.Tests.Users` (a one-line `SetUpFixture : E2ESetupFixtureBase`, `appsettings.json`, a
`Base<Module>Tests`) and register its client in `ClientsServiceRegister` + `E2ESetupFixtureBase`.

**Within the project, tests are grouped by controller in subfolders.** The subfolder name matches the
controller's primary resource: `Users/`, `Roles/`, `Permissions/`, etc. Cross-resource controllers
(e.g. `RolePermissions`, which assigns permissions to a role) belong to the primary resource's folder
(`Roles/`). The subfolder becomes the last segment of the namespace:
`Dominodo.E2E.Tests.Users.Roles`, `Dominodo.E2E.Tests.Users.Users`, etc.

## Step 3 — Models (hand-written, `Model` suffix)

In `Modules/<Module>/Models/`, one record per request/response, mirroring the API DTO **by value**.
Reuse `CreatedModel`, `PagedResultModel<T>`, `ProblemDetailsModel` from `Clients.Core`. Naming:
`New<Noun>Model` (create), `Update<Noun>Model` (edit), `<Noun>Model` (response), `<Noun>FilterModel`
(query string). Use `sealed record` with `init` setters so tests override via `model with { ... }`
(e.g. `RoleModel { int Id; string Name; string? Description }`).

## Step 4 — Refit client method (`I<Module>Client`)

Hand-written versioned route. Token flows via `[Authorize("Bearer")]` (**null ⇒ anonymous**, which is
how you test 401). Return `ApiResponse<T>` (or `IApiResponse` when you only assert the status).

```csharp
[Get("/api/v1/roles")]
Task<ApiResponse<PagedResultModel<RoleModel>>> GetRoles(
    [Query] int page = 1,
    [Query] int pageSize = 20,
    [Authorize("Bearer")] string? token = null);
```

## Step 5 — RequestBuilder (the Arrange)

`<Module>RequestBuilder : BaseRequestBuilder`, injected the module's client. Per entity, **three members**:
- `Build<Noun>Model(...params)` — valid fake data (`Faker.E164Phone()`, `Faker.StrongPassword()`,
  `DominodoFakerExtensions`), every field overridable. **Does NOT call the API.**
- `<Create><Noun>Async(...params)` — parameter overload; calls `Build<Noun>Model(...params)` then delegates
  to the model overload. Lets a test override one field, e.g. `RegisterUserAsync(password: pwd)`.
- `<Create><Noun>Async(<Noun>Model model)` — the API call: creates, **reads back via `GET /...{id}`, returns
  the persisted model** (`UserModel`, …), and **throws on any non-success step** (create or read-back).

`model` is **required** (no default) so the no-arg call resolves to the parameter overload. Composite
helpers (e.g. `CreateUserAndAuthenticateAsync`) build on the model overloads. Register the builder in
`ClientsServiceRegister.Add<Module>Client()`.

## Step 6 — The test class

`<Verb><Noun>Tests : Base<Module>Tests` (which exposes `<Module>Client`, `<Module>RequestBuilder`, and
inherited `JwtTokenFactory`, `Faker`, `Fixture`). Structure every test **Arrange → Act → Assert**:
- **Arrange** with builders only.
- **Act** = exactly **one** call to the client under test. The client appears **only** here.
- **Assert** on `StatusCode`, `Content`, and/or the `ProblemDetailsModel` via
  `ShouldHaveValidationError(prop)` / `ShouldHaveErrorCode(code)`.

Auth in Act: `JwtTokenFactory.CreateSuperAdminToken()` for an authorized call; a plain
`CreateUserToken(Guid.NewGuid(), "SomeRole")` for a token that lacks a permission (→ 403); no token
(null) for 401.

```csharp
// File: tests/e2e/tests/Dominodo.E2E.Tests.Users/Roles/GetRolesTests.cs
namespace Dominodo.E2E.Tests.Users.Roles;   // subfolder = last namespace segment

[TestFixture]
public sealed class GetRolesTests : BaseUsersTests
{
    [Test]
    public async Task _401_WhenAnonymous()
    {
        var response = await UsersClient.GetRoles();                       // Act — no token
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
    [Test]
    public async Task _403_WhenUserLacksManageRoles()
    {
        var token = JwtTokenFactory.CreateUserToken(Guid.NewGuid());       // valid, no role/permission
        var response = await UsersClient.GetRoles(token: token);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
    [Test]
    public async Task _400_WhenPaginationInvalid()
    {
        var token = JwtTokenFactory.CreateSuperAdminToken();
        var response = await UsersClient.GetRoles(page: 0, pageSize: -1, token: token);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ShouldHaveErrorCode("Validation.Failed");
    }
    [Test]
    public async Task _200_ReturnsRoles()
    {
        var token = JwtTokenFactory.CreateSuperAdminToken();
        var response = await UsersClient.GetRoles(token: token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content!.Items.ShouldNotBeEmpty();
    }
}
```

## Step 6a — A `400` test must cover the **whole** validator

Read the request's validator and list every `RuleFor` constraint (`.NotEmpty()`/`.NotNull()`,
`.MaximumLength(n)` → `new string('x', n+1)`, `.Must(...)`/enum-parse → a value that fails it, …).
**Prefer one test** that breaks every field at once and asserts each error together — cleaner than a test
per field:

```csharp
var model = UsersRequestBuilder.BuildNewRoleModel() with
    { Name = "", Description = new string('x', 301), Scope = "NotAScope", PermissionIds = null };
var response = await UsersClient.CreateRole(model, token);
response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
response.ShouldHaveValidationError(nameof(NewRoleModel.Name))
        .ShouldHaveValidationError(nameof(NewRoleModel.Description))
        .ShouldHaveValidationError(nameof(NewRoleModel.Scope))
        .ShouldHaveValidationError(nameof(NewRoleModel.PermissionIds));
```

Add a **second** test only for a rule that can't coexist in the same payload (mutually exclusive
constraints on one field, e.g. empty vs. too-long `Name`). The point is coverage of every rule — not one
test per rule.

## Non-negotiables (checklist)

- [ ] **Zero changes under `src/`** (Rule 0) — arrange via `ISqlClient`, never new API code.
- [ ] Models replicated **by hand**, not referenced/copied from `src/` or generated from OpenAPI.
- [ ] Client used **only** inside `Act`; all Arrange via the builder.
- [ ] Each entity has the **three builder members**: `Build<Noun>Model(...)`, `<Create>Async(...params)`
      (delegates to the model overload), and `<Create>Async(model)` (creates, reads back, returns the model).
- [ ] Create helpers **read the entity back via GET and return the persisted model**, and **throw on
      non-success** at every step (create *and* read-back).
- [ ] Test names `_<status>_<scenario>`; class `<Verb><Noun>Tests`.
- [ ] The `400` test(s) cover **every** rule in the request's validator (ideally one test breaking all
      fields at once, not just the first field). Cross-check against `<Command>Validator.cs` — see Step 6a.
- [ ] Conflict/duplicate tests build their own unique prerequisite (fresh fake data) — tests share one
      DB, so isolate by per-test data, never by resets or fixed state.
- [ ] Cross-module effects (integration events) asserted via `RetryPolicies.Until<T>` polling, never an
      immediate assert.

## Run it

The API + SQL Server must be up. Bring up SQL Server (`docker compose up -d --wait`; local port
**1435**), run the API in Development (`dotnet run --project src/Bootstrap/Dominodo.Api`), point the E2E
`appsettings.json`/`appsettings.Local.json` `BaseUrl` at it, and set the `Jwt` triple to **match the
running environment** so minted tokens validate. Then: `dotnet test tests/e2e/Dominodo.E2E.sln`.
