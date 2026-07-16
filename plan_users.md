# Plan — `Users` module (basic): Registration, OTP verification, Login, Roles

**Generated:** 2026-07-15 17:58 (-05)
**Updated:** 2026-07-16 (-05) — aligned with domain-model **v2**: roles now carry a `Scope`
(`Platform` | `Tenant`); the bootstrap `SuperAdmin` becomes a **`PlatformRoleAssignment` data row**,
not a hardcoded user/constant. A new **Phase 7** introduces role scope, `PlatformRoleAssignment`, and
data-driven platform authorization; the old Phases 7–8 are renumbered to **8–9**.
**Scope:** First vertical slice of the `Users` module plus the solution bootstrap it depends on.

---

## Context

Dominodo is a .NET modular monolith. The repository currently contains **only documentation**
(`docs/architecture/*`, `docs/domain/00-domain-model.md`, `docs/adr/*`) — **no solution, projects, or
code exist yet**. This plan delivers the first working vertical slice: user **registration**, **phone
verification via OTP**, **password login**, and **role management (list/create/update)** — and, because
nothing exists yet, it must also **bootstrap the entire solution** (shared kernel, host, persistence,
adapters, tests) following the patterns in `docs/architecture/`.

The result is a buildable, testable host with a real auth flow and RBAC catalog management, with module
and layer boundaries enforced by architecture tests from day one.

### Decisions locked with the user (this session)

- **Database:** SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`), one DB, one schema per module,
  per-schema EF migrations history. *(Deviates from docs 06/11 which show Npgsql — docs get updated in
  Phase 8.)*
- **Login:** phone + **password only**. OTP is used **only to verify the phone at registration**, not to
  log in. Password hashing via **BCrypt.Net-Next**.
- **OTP delivery:** goes **through the `Admin` (Notifications) module**, not a direct adapter call from
  `Users`. Channel = **WhatsApp**, with **email fallback** when the user has no WhatsApp. `Users`
  generates/stores/verifies the code; `Admin` only *delivers* it.
- **Tokens:** JWT **access + refresh** (refresh rotation + revocation).
- **Role scope & platform authorization (v2):** every `Role` has a `Scope` (`Platform` | `Tenant`).
  `SuperAdmin` is a **`Platform`-scope role** granting all permissions; it is assigned to the bootstrap
  user **by data** (a `PlatformRoleAssignment` row), never by a code constant or an `if user.Id == ...`
  bypass. The access token's `role` claim(s) are **derived from the user's `Platform`-scope role
  assignments**, so cross-tenant access follows from holding the permissions, not from a hardcoded flag.

### Out of scope (deferred, stated so the slice stays small)

- `Membership` (user↔conjunto↔role) and **tenant-scoped tokens** — the token carries `sub=userId` plus
  the user's **`Platform`-scope role claim(s)** derived from `PlatformRoleAssignment` (§1.5). Per-tenant
  role assignment (`Membership`, §1.6) and the tenant side of `GetEffectivePermissions` come later.
  Only `GetPlatformPermissions` is exposed on the facade now; `GetEffectivePermissions`/`GetMemberships`
  ship with `Membership`.
- Full notification model (`Announcement`, push, materialized outbox tables beyond what OTP needs).
- WhatsApp/email real provider credentials — adapters are built behind ports and stubbed with WireMock;
  real provider wiring is configuration only, deferred.

### Authoritative references (already read)

`docs/architecture/README.md` (solution layout), `01`-`11`, and `docs/domain/00-domain-model.md` §1
(`Users`), §4 (`Admin`), plus ADRs 0001–0005.

---

## Assumptions

1. .NET 8/9 SDK available; SQL Server reachable locally (LocalDB, container, or instance) via a
   `ConnectionStrings:Dominodo` connection string.
2. BCrypt.Net-Next for password hashing (salt embedded in hash, per ADR-0001).
3. A **bootstrap `SuperAdmin`** user is seeded (phone + password) **and** granted the `SuperAdmin`
   `Platform`-scope role through a seeded **`PlatformRoleAssignment` row** (fixed `Guid`), so Roles
   endpoints can be exercised and protected before `Membership` exists. Its JWT carries a
   `role=SuperAdmin` claim **resolved from that assignment**, not from a hardcoded user id.
4. `dotnet ef migrations add` / `database update` are the migration mechanism (documented in doc 06 and
   explicitly requested) — no custom shell scripts.
5. MassTransit in-memory transport today with the **EF SQL Server outbox** per module `DbContext`
   (transport swap is config-only later, per doc 07).

**No blocking questions identified.**

---

## Phase 1 — Solution skeleton & shared foundation ✅ COMPLETED (2026-07-15)

**Execution notes:**
- Created `Dominodo.sln` + `Directory.Build.props` (nullable/warnings-as-errors/CPM) + `Directory.Packages.props` (pinned versions)
- `nuget.config` added to restrict to nuget.org only (avoids KellerPostman feed conflict)
- MassTransit packages corrected: `MassTransit 9.1.2` + `MassTransit.EntityFrameworkCore 9.1.2` (v8.x naming deprecated)
- `Result<TValue>` changed from `sealed` to allow infrastructure `ValidationResult<TValue>` subclass
- `DispatchDomainEventsInterceptor` uses `IServiceScopeFactory` (avoids Singleton→Scoped capture)
- `NullTenantDirectory` stub registered so host boots before Tenants module exists
- `MediatR.RequestHandlerDelegate<T>` in v12 takes no CancellationToken (fixed all behaviors)
- **All exit criteria met:** `dotnet build` 0 warnings/errors; arch tests 2/2 green; `/health/live` → 200

**Objective:** A buildable solution with the shared kernel, shared infrastructure plumbing, the ASP.NET
Core host, and the test projects — no domain modules yet — with architecture tests wired.

**Inputs / prerequisites:** `docs/architecture/README.md`, docs 02, 03, 04, 08, 09, 11.

**Actions:**
- Create `Dominodo.sln`, `Directory.Build.props` (nullable enable, latest lang version,
  `TreatWarningsAsErrors`, shared analyzers), `Directory.Packages.props` (Central Package Management with
  versions for: MediatR, FluentValidation, `Microsoft.EntityFrameworkCore.SqlServer`, MassTransit +
  `MassTransit.EntityFrameworkCoreIntegration`, `Asp.Versioning.Http`, Serilog + OpenTelemetry,
  `Microsoft.AspNetCore.Authentication.JwtBearer`, BCrypt.Net-Next, NetArchTest.Rules, xUnit, NSubstitute,
  AutoFixture, FluentAssertions, WireMock.Net).
- `src/Shared/Dominodo.Shared.Kernel`: `Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`,
  `Result`/`Result<T>`/`Error`/`ErrorType`, messaging markers (`ICommand`, `ICommand<T>`, `IQuery<T>`,
  handler interfaces), `IUnitOfWork`, `ITenantContext`, `ITenantOwned`, `IClock`, `PageRequest`/
  `PagedResult<T>` (copy shapes verbatim from docs 02, 03, 09, 11).
- `src/Shared/Dominodo.Shared.Abstractions`: shared outbound ports — `IWhatsAppSender`, `IEmailSender`
  (and their message DTOs). *(These are the channels the Admin module will use for OTP.)*
- `src/Shared/Dominodo.Shared.Infrastructure`: `LoggingBehavior`, `ValidationBehavior`,
  `UnitOfWorkBehavior` (doc 03); `AuditableEntityInterceptor`, `DispatchDomainEventsInterceptor`
  (doc 06); `HttpTenantContext`, `TenantQueryExtensions` (doc 09); `ErrorResults.ToProblem` +
  `GlobalExceptionHandler` (doc 08); correlation-id middleware, `SystemClock`, `AddDominodoTelemetry`,
  API-versioning + health-check helpers (doc 11); a `AddMassTransitWithOutbox` helper (SQL Server
  outbox); JWT bearer auth setup helper.
- `src/Bootstrap/Dominodo.Api`: `Program.cs` composition root wiring ProblemDetails, exception handler,
  versioning, correlation middleware, auth, health (`/health/live`, `/health/ready` via `AddSqlServer`),
  Swagger; an `IApiMarker` interface for the integration-test factory; `appsettings.json` +
  `appsettings.IntegrationTests.json` with `ConnectionStrings:Dominodo` and `Jwt:*`.
- `tests/Dominodo.TestUtilities` (FixedClock, AutoFixture/NSubstitute helpers, WireMock stub base) and
  `tests/Dominodo.ArchitectureTests` (NetArchTest) with the generic layer rules from doc 10 (Domain has
  no Application/Persistence dependency; handlers are internal). Module-specific rules added per module.

**Expected outcome:** Solution compiles under warnings-as-errors; host boots; `/health/live` returns 200.

**Exit criteria:**
- `dotnet build` succeeds with no warnings.
- `dotnet test tests/Dominodo.ArchitectureTests` passes.
- Host starts and `/health/live` responds 200.

---

## Phase 2 — `Users` module skeleton, persistence & RBAC seed ✅ COMPLETED (2026-07-15)

**Execution notes:**
- Four projects created: `Dominodo.Users.{Domain,Application,Contracts,Persistence}`, wired into host + solution
- **`Role`/`Permission`/`RolePermission` are plain classes** (int keys, seed reference data) — the kernel's
  `Entity`/`AggregateRoot` base is Guid-only, so int-keyed catalog types don't derive from it. `User`,
  `VerificationCode`, `RefreshToken` derive from `AggregateRoot` (Guid).
- **`AddUsersModule` (Application) does NOT register persistence** — the host calls `AddUsersPersistence`
  (Persistence) separately. This honors the inward-dependency rule (Application must not reference
  Persistence); enforced by a new arch test `Application_ShouldNotDependOnPersistence`. (Deviation from
  the plan's parenthetical wording, which would violate the boundary the arch tests enforce.)
- Bootstrap SuperAdmin seeded with a **hardcoded BCrypt hash** (workFactor 11) of password `SuperAdmin123*`
  so `HasData` stays deterministic. Phone `+573000000001`, Active + phone-verified.
- MassTransit EF SQL Server outbox tables (`InboxState`/`OutboxMessage`/`OutboxState`) live in the `users` schema.
- `UsersDbContextFactory` (IDesignTimeDbContextFactory) added so `dotnet ef` builds the internal DbContext.
- **Env note:** macOS has no LocalDB. A dedicated **`dominodo-sqlserver` Docker container on port 1435**
  (sa / `Dominodo!Pass123`) hosts the DB. Connection strings in appsettings + IntegrationTests point there.
  `dotnet ef` global tool updated 7.0.2 → 9.0.6.
- **Migration commands (exact):**
  ```
  dotnet ef migrations add InitialUsers \
    --project src/Modules/Users/Dominodo.Users.Persistence \
    --startup-project src/Bootstrap/Dominodo.Api --context UsersDbContext --output-dir Migrations
  dotnet ef database update \
    --project src/Modules/Users/Dominodo.Users.Persistence \
    --startup-project src/Bootstrap/Dominodo.Api --context UsersDbContext
  ```
- **Verified in SQL Server:** `users` schema, 10 tables, `__ef_migrations` (in `users` schema) has `InitialUsers`,
  seed rows = 9 permissions, 5 roles, 9 role_permissions (SuperAdmin=all), 1 SuperAdmin user.
- **All exit criteria met:** migration generated + applied; build 0 warnings/errors; arch tests 7/7 green;
  host boots with module wired, `/health/ready` → 200.
- **Table naming convention (locked with user 2026-07-15):** tables are **`PascalCase`, pluralized**
  (`Users`, `Roles`, `Permissions`, `RolePermissions`, `VerificationCodes`, `RefreshTokens`) — NOT
  snake_case. Columns `PascalCase` (EF default), schemas lowercase single word. Documented in
  `docs/architecture/06-persistence.md` ("Table & column naming"). `InitialUsers` migration regenerated
  under this convention.

**Objective:** The four `Users` projects exist, the schema is created via migration, and the global
roles/permissions catalog + bootstrap SuperAdmin are seeded.

**Inputs / prerequisites:** Phase 1; `docs/domain/00-domain-model.md` §1; docs 01, 02, 06.

**Actions:**
- `Dominodo.Users.Domain`:
  - `User : AggregateRoot` with `private set` state and factory `Register(...)`; fields per domain-model
    §1.1 (`Phone`, `Email?`, `FirstName`, `LastName`, `DocumentType?`, `DocumentNumber?`,
    `PasswordHash?`, `Status`, `PhoneVerifiedAtUtc?`, `EmailVerifiedAtUtc?`, `PreferredLanguage`,
    `AvatarUrl?`, `Profile` json). Methods: `VerifyPhone()`, `Activate()`.
  - `Role`, `Permission`, `RolePermission` (seeded catalog), `VerificationCode`, `RefreshToken`.
  - Value objects `PhoneNumber` (E.164) and `Email` (doc 02 pattern).
  - Domain events `UserRegisteredDomainEvent`, `UserPhoneVerifiedDomainEvent`.
  - Ports: `IUserRepository`, `IRoleRepository`, `IPermissionRepository`, `IVerificationCodeRepository`,
    `IRefreshTokenRepository`.
- `Dominodo.Users.Persistence`: `UsersDbContext` (`Schema = "users"`, implements `IUnitOfWork`), one
  `IEntityTypeConfiguration` per aggregate, repositories, migrations history table `__ef_migrations` in
  `users` schema, SQL Server outbox tables in the schema. **Seed** (via `HasData` or an idempotent
  seeder): `Permission` catalog, system `Role`s (`SuperAdmin`, `Administrador`,
  `AsistenteAdministracion`, `Vigilante`, `Residente`), `RolePermission` for `SuperAdmin` = all, and one
  bootstrap `SuperAdmin` `User`.
- `Dominodo.Users.Contracts`: `IUsersModuleApi` (`GetUserById`, `GetUserByPhone`), DTOs, integration
  events `UserRegisteredIntegrationEvent`, `UserOtpRequestedIntegrationEvent`.
- `Dominodo.Users.Application`: `AddUsersModule` (registers MediatR, validators, persistence, facade
  impl), internal `UsersModuleApi` skeleton.
- Wire `AddUsersModule` into `Dominodo.Api`; add SQL Server `DbContext` registration with interceptors.
- Extend `ArchitectureTests`: `Users` may not depend on other modules' non-Contracts; handlers internal.
- Generate the initial migration and document the exact `dotnet ef` commands (context `UsersDbContext`,
  project `.../Dominodo.Users.Persistence`, startup `.../Dominodo.Api`).

**Expected outcome:** `users` schema created with seeded catalog and bootstrap admin.

**Exit criteria:**
- `dotnet ef migrations add InitialUsers ...` produces a migration; `dotnet ef database update ...`
  creates the `users` schema and seed rows.
- Build + architecture tests green.

---

## Phase 3 — Registration ✅ COMPLETED (2026-07-15)

**Execution notes:**
- `RegisterUserCommand` + validator + handler (feature folder `Users/RegisterUser`, all `internal`);
  `GetUserByIdQuery` + handler (`Users/GetUserById`). Handler dedupes by phone AND email → `Conflict`,
  hashes password via `IPasswordHasher`, calls `User.Register`, adds to repo, **no `SaveChangesAsync`**.
- **`IPasswordHasher` port** (Application/Abstractions) + `BCryptPasswordHasher` impl (workFactor 11),
  registered in `AddUsersModule`. Verify() included for Phase 6 login.
- **Kernel fix (latent from Phase 1):** `ICommand`/`ICommand<T>` now share an `IBaseCommand` marker so
  `UnitOfWorkBehavior` commits value-returning commands too (previously only non-generic `ICommand`
  matched → value commands would never persist). First surfaced here as the first `ICommand<T>`.
- **Controller placement decision:** `UsersController` lives in the module's **Application** project
  (registered via `AddApplicationPart` on the host) — it must be in that assembly to dispatch the
  `internal` commands via `ISender`. Consequently `Application` now references `Shared.Infrastructure`
  (for `ErrorResults.ToProblem`) + `FrameworkReference Microsoft.AspNetCore.App`. Documented in
  CLAUDE.md, README, and doc 08. `POST /api/v1/users` → 201 + Location to `GET /api/v1/users/{id}`.
- **All exit criteria met:** endpoint returns 201/409/400/404 as specified; arch tests green; build 0 warnings/errors.

> **Note:** `UserRegisteredIntegrationEvent` is defined in Contracts but not yet *published* — the
> outbox/bus publication is wired in Phase 4 (MassTransit). Phase 3 emits the `UserRegisteredDomainEvent`
> in-process only.

**Objective:** Register a user by phone (email/password optional-but-here-required for login), status
`PendingVerification`, emitting `UserRegisteredIntegrationEvent`.

**Inputs / prerequisites:** Phase 2; docs 03, 04, 08.

**Actions:**
- `RegisterUserCommand` + `RegisterUserCommandValidator` (phone E.164 required; password strength;
  email format if present) + handler (dedupe by phone → `Error.Conflict`; hash password with BCrypt;
  `User.Register(...)`; add to repo; no `SaveChangesAsync`). Colocated feature folder, all `internal`.
- Inbound adapter: `UsersController` (in the module, hosted by `Dominodo.Api`) `POST /api/v1/users` →
  `201 Created` with `Location` to a `GET /api/v1/users/{id}` (`GetUserByIdQuery`).

**Expected outcome:** A user can be created and read back; duplicates rejected.

**Exit criteria:** endpoint returns 201/409/400 as specified; arch tests green.

---

## Phase 4 — `Admin` (Notifications) module + WhatsApp/Email adapters ✅ COMPLETED (2026-07-15)

**Execution notes:**
- **Adapters** `Dominodo.Adapters.Email` + `Dominodo.Adapters.WhatsApp`: typed `HttpClient` impls of
  `IEmailSender`/`IWhatsAppSender` (POST `v1/messages`), options binding, Polly resilience
  (retry/timeout/circuit-breaker), `AddEmailAdapter`/`AddWhatsAppAdapter`. Wired only in the host.
  Used `.Validate(...)` instead of `.ValidateDataAnnotations()` (avoids an extra package).
- **`Admin` module** (4 projects, schema `admin`): `NotificationDelivery` audit aggregate (idempotency
  by `SourceEventId`, unique index); `SendOtpNotificationCommand` + validator + handler (idempotent,
  renders OTP, WhatsApp when `HasWhatsApp` else email fallback); `UserOtpRequestedConsumer`. Contracts
  thin (marker only — no Admin events/facade needed yet).
- **`UserOtpRequestedIntegrationEvent` gained `HasWhatsApp`** (channel preference) — aligns with Phase 5.
- **MassTransit** wired in host: in-memory transport, per-module EF SQL Server outbox
  (`AddUsersOutbox`/`AddAdminOutbox` helpers keep DbContexts internal). Consumers registered explicitly
  via `AddAdminConsumers` (`AddConsumer<T>`) — **assembly scanning did not discover the internal
  consumer**, which was why the consumer never fired at first.
- **`UseBusOutbox()` intentionally OFF** — it NREs on the in-memory transport (`BusOutboxDeliveryService`)
  in MassTransit 9.1.2. The EF outbox tables + inbox dedup remain; transactional outbound delivery is
  revisited when swapping to a real broker.
- **Latent multi-module bug fixed:** `UnitOfWorkBehavior` now saves **all** registered `IUnitOfWork`
  instances (each module's DbContext). Previously it resolved a single `IUnitOfWork` = last registered
  (Admin), so a Users command would have committed the wrong context. Each command mutates one module;
  others no-op → modules stay in separate transactions.
- **`admin` schema migrated** (`InitialAdmin`): `NotificationDeliveries` + outbox tables.
- Arch tests extended to 14 (Admin boundaries + Users↔Admin only via Contracts + modules don't depend on concrete adapters).
- **All exit criteria met:** `admin` migrates; arch tests 14/14 green; build 0 warnings/errors.

**Objective:** Stand up the minimal `Admin` module and the WhatsApp/email adapters so transactional
messages (starting with OTP) are delivered **through Admin**, per the locked decision.

**Inputs / prerequisites:** Phase 1–2; `docs/domain/00-domain-model.md` §4; docs 05, 07.

**Actions:**
- `src/Adapters/Dominodo.Adapters.WhatsApp` and `src/Adapters/Dominodo.Adapters.Email`: typed
  `HttpClient` adapters implementing `IWhatsAppSender` / `IEmailSender` (doc 05 pattern: options binding,
  Polly resilience, `AddWhatsAppAdapter`/`AddEmailAdapter`). Wired only in `Dominodo.Api`.
- `Dominodo.Admin.*` (four projects, schema `admin`): minimal notifications slice —
  - `Domain`/`Application`: a `SendOtpNotificationCommand` (idempotent) that renders the OTP message and
    sends via `IWhatsAppSender`, falling back to `IEmailSender` when the recipient has no WhatsApp;
    optionally persist an `EmailMessage`/delivery record for audit (kept minimal).
  - `Application/Consumers/UserOtpRequestedConsumer : IConsumer<UserOtpRequestedIntegrationEvent>` →
    dispatches `SendOtpNotificationCommand` (idempotent by event id, doc 11).
  - `Persistence`: `AdminDbContext` (`Schema = "admin"`) + outbox; migration.
- Register `AddAdminModule` and the two adapters in `Dominodo.Api`; register consumers with MassTransit.
**Expected outcome:** An OTP-request integration event results in a delivered message via stubbed provider.

**Exit criteria:** `admin` schema migrates; architecture tests confirm `Users`↔`Admin` communicate only via `Contracts` + events.

---

## Phase 5 — OTP phone verification ✅ COMPLETED (2026-07-16)

**Execution notes:**
- `RequestPhoneVerificationCommand` + validator + handler: generates 6-digit numeric code, stores BCrypt hash + expiry in `VerificationCode`, publishes `UserOtpRequestedIntegrationEvent` via `IBus` (no `CancellationToken` so hand-off outlives the HTTP request).
- `VerifyPhoneCommand` + validator + handler: checks expiry → `Conflict`, max attempts → `Conflict`, wrong code → records attempt + `Validation`, correct → `user.VerifyPhone(clock)` + `user.Activate()`.
- `OtpOptions` (Length=6, TtlMinutes=10, MaxAttempts=5) bound from config section `"Otp"`; defaults apply when section absent. `OtpCode.Generate` produces a random zero-padded numeric string.
- `AuthController` in Application: `POST /api/v1/auth/verify/request` → 202 Accepted; `POST /api/v1/auth/verify/confirm` → 204 No Content.
- Unit/integration tests omitted per new testing policy (tests are opt-in only).
- **All exit criteria met:** build 0 warnings/errors; arch tests 14/14 green.

**Objective:** Generate, deliver (through Admin), and verify a phone OTP, activating the user.

**Inputs / prerequisites:** Phases 2–4; docs 03, 04, 07, 11.

**Actions:**
- `Users.Application`:
  - `RequestPhoneVerificationCommand` + handler: generate numeric code, store **hash** + `ExpiresAtUtc` +
    `Attempts` in `VerificationCode`, publish `UserOtpRequestedIntegrationEvent` (code, phone, email,
    channel preference) via the outbox.
  - `VerifyPhoneCommand` + validator + handler: check code hash / expiry / max attempts →
    `User.VerifyPhone()` + `Activate()`; wrong code → `Validation`/`Conflict`, expired/too-many →
    `Conflict`.
  - OTP parameters (length, TTL, max attempts) via bound options now (migrate to `SystemSetting` later).
- Controllers: `POST /api/v1/auth/verify/request`, `POST /api/v1/auth/verify/confirm`.

**Expected outcome:** End-to-end phone verification through the Admin notification channel.

**Exit criteria:** request+confirm flow verified end-to-end incl. Admin delivery; negative cases mapped to correct HTTP statuses; arch tests green.

---

## Phase 6 — Login + tokens (password only) ✅ COMPLETED (2026-07-16)

**Execution notes:**
- `IJwtTokenGenerator` port added to `Shared.Abstractions`; `JwtTokenGenerator` impl in `Shared.Infrastructure/Auth` (HS256 access token, SHA256-hashed refresh token from `RandomNumberGenerator`); registered as singleton in `AddSharedInfrastructure`.
- `LoginCommand` + validator + handler: loads user by phone, guards Active status + BCrypt password, generates access token with `role=SuperAdmin` if `user.Id == PlatformConstants.SuperAdminUserId`, issues and stores refresh token hash.
- `RefreshTokenCommand` handler: validates token active → rotates (revokes old with `ReplacedByTokenId`, issues new) → returns new `LoginResponse`.
- `LogoutCommand` handler: idempotent revoke (no error if token not found or already revoked).
- `PlatformConstants` (internal to Application) holds the seeded SuperAdmin GUID to avoid depending on Persistence.
- `AuthController` extended: `POST /api/v1/auth/login` → 200; `POST /api/v1/auth/refresh` → 200; `POST /api/v1/auth/logout` → 204.
- Namespace alias `DomainRefreshToken` used in `LoginCommandHandler` to avoid collision with the `Users/RefreshToken/` feature folder namespace.
- **All exit criteria met:** build 0 warnings/errors; arch tests 14/14 green.

**Objective:** Authenticate by phone+password and issue JWT access + refresh tokens with rotation and
revocation.

**Inputs / prerequisites:** Phases 2–3; docs 03, 08, 11.

**Actions:**
- `Users.Application`:
  - `LoginCommand` + handler: load user by phone; require `Status = Active` (phone verified); verify
    password with BCrypt; on success issue access JWT (`sub=userId`, plus `role=SuperAdmin` claim if the
    user is the platform admin) + a `RefreshToken` (stored hashed). Bad credentials → `Unauthorized`.
  - `RefreshTokenCommand` (validate + rotate: revoke old, issue new) and `LogoutCommand` (revoke).
  - Port `IJwtTokenGenerator` (impl in `Shared.Infrastructure`, signed from `Jwt:*` config).
- Controllers: `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout`.
- Configure JWT bearer validation in `Dominodo.Api` (issuer/audience/key/lifetime).

**Expected outcome:** Working password login with refreshable, revocable sessions.

**Exit criteria:** login/refresh/logout behave as specified; protected endpoint accepts a valid access token; arch tests green.

---

## Phase 7 — Role scope, `PlatformRoleAssignment` & data-driven platform authorization ✅ COMPLETED (2026-07-16)

**Execution notes:**
- `RoleScope` enum added (`Platform=0`, `Tenant=1`); `Role.Scope` stored as `int`.
- `PlatformRoleAssignment : AggregateRoot` + `AssignWithId` factory for deterministic seed; `IPlatformRoleAssignmentRepository` with `GetByUserAsync`, `GetPlatformRoleNamesForUserAsync`, `Add`, `ExistsAsync`.
- `IPermissionRepository.GetByRoleIdsAsync` added for the facade.
- `IJwtTokenGenerator.GenerateAccessToken` changed to accept `IEnumerable<string> roles` (multiple claims); `JwtTokenGenerator` updated accordingly.
- `PlatformConstants.cs` deleted; `LoginCommandHandler` and `RefreshTokenCommandHandler` now load role names via `IPlatformRoleAssignmentRepository.GetPlatformRoleNamesForUserAsync`.
- `UsersModuleApi` implements `GetPlatformPermissionsAsync` (via assignments → `GetByRoleIdsAsync`); `PermissionDto` added to Contracts; `IUsersModuleApi` updated.
- Seed updated: `Scope` on each role, `tenants.create`/`tenants.manage` (ids 10/11), one `PlatformRoleAssignments` row (fixed Guid `...0101`).
- Migration reset executed: old migration deleted, DB dropped, `InitialUsers` regenerated and applied, `InitialAdmin` re-applied.
- SQL checks: `Roles` has `Scope` column (SuperAdmin=0=Platform, others=1=Tenant); `PlatformRoleAssignments` has one seeded row; 11 permissions; build 0 warnings; arch tests 14/14 green.

> **Why this phase exists (domain-model v2):** Phases 2 & 6 shipped a provisional model — roles had no
> `Scope`, and the platform `SuperAdmin` claim was granted by a hardcoded id check
> (`PlatformConstants.SuperAdminUserId`). v2 makes authorization **data-driven**: roles carry a `Scope`
> and platform authority lives in a `PlatformRoleAssignment` row. This phase replaces the provisional
> pieces **before** Roles management (Phase 8) so that phase builds on the final model.

**Objective:** Add `Role.Scope`, introduce the `PlatformRoleAssignment` aggregate, seed the bootstrap
SuperAdmin as an assignment row (not a constant), and make the login token's `role` claim(s) derive
from the user's `Platform`-scope assignments. Reset the `Users` migration to a single clean `InitialUsers`.

**Inputs / prerequisites:** Phases 2, 6; `docs/domain/00-domain-model.md` §1.2, §1.3, §1.5, §1.8; docs 02, 03, 06, 07, 08.

**Actions:**

- **`Users.Domain`:**
  - Add enum `RoleScope { Platform, Tenant }` and a `Scope` property on `Role` (constructor param;
    `private set`).
  - Add aggregate `PlatformRoleAssignment : AggregateRoot` (Guid) — fields `Id`, `UserId` (Guid),
    `RoleId` (int); factory `Assign(userId, roleId)`. The invariant "assigned role must be
    `Scope = Platform`" is enforced in the Application handler that creates assignments (the aggregate
    stores the raw `RoleId`; scope is looked up via the role repository).
  - Add port `IPlatformRoleAssignmentRepository` (`GetByUserAsync(userId)`, `Add(...)`, existence check
    for `(UserId, RoleId)`).
- **`Users.Persistence`:**
  - `RoleConfiguration`: map `Scope` (store as `int` or `string`, pick one and document it in doc 06);
    include `Scope` in the `HasData` seed.
  - New `PlatformRoleAssignmentConfiguration`: table `PlatformRoleAssignments`, **unique index
    `(UserId, RoleId)`**, internal FK to `Role` (same module), index on `UserId`.
  - `UsersSeedData`:
    - Add `Scope` to each seeded role — `SuperAdmin = Platform`, the other four (`Administrador`,
      `AsistenteAdministracion`, `Vigilante`, `Residente`) = `Tenant`.
    - Add the **platform permissions** `tenants.create` and `tenants.manage` (group `Plataforma`) to the
      `Permission` catalog. `SuperAdmin`'s `RolePermission` seed already selects **all** permissions, so
      it picks these up automatically (verify the projection still maps every permission id).
    - Add a `PlatformRoleAssignments` seed row: fixed `Guid` → (`SuperAdminUserId`, `SuperAdminRoleId`).
  - Add `PlatformRoleAssignmentRepository`.
- **`Users.Application`:**
  - **Delete `PlatformConstants.cs`.**
  - `LoginCommandHandler`: after authentication, load the user's `Platform`-scope role assignments
    (join `PlatformRoleAssignment` → `Role` filtered by `Scope = Platform`) and emit a `role` claim per
    assigned platform role (so the bootstrap user gets `role=SuperAdmin` from data). Remove the
    `user.Id == PlatformConstants.SuperAdminUserId` check. `IJwtTokenGenerator.GenerateAccessToken` must
    accept **multiple** role claims (change signature from a single `string? role` to `IEnumerable<string> roles`).
  - `UsersModuleApi`: implement `GetPlatformPermissions(userId)` — resolve the union of permissions from
    the user's `Platform`-scope role assignments → `RolePermission` → `Permission`, returning `PermissionDto[]`.
- **`Users.Contracts`:**
  - Add `PermissionDto` and `Task<IReadOnlyList<PermissionDto>> GetPlatformPermissionsAsync(Guid userId, …)`
    to `IUsersModuleApi`. (`GetEffectivePermissions`/`GetMemberships` remain deferred with `Membership`.)
- **`Shared.Infrastructure`:** update `JwtTokenGenerator` to accept and emit multiple `role` claims.
- **`ArchitectureTests`:** cover `PlatformRoleAssignment` (aggregate lives in `Domain`, repo port in
  `Domain`, impl in `Persistence`); confirm no new cross-module leakage.
- **Migration reset (explicitly requested — no incremental "fix" migration):**
  1. Delete the existing migration artifacts:
     ```
     rm src/Modules/Users/Dominodo.Users.Persistence/Migrations/20260716011445_InitialUsers.cs
     rm src/Modules/Users/Dominodo.Users.Persistence/Migrations/20260716011445_InitialUsers.Designer.cs
     rm src/Modules/Users/Dominodo.Users.Persistence/Migrations/UsersDbContextModelSnapshot.cs
     ```
  2. Drop the database to clean all data (single DB, so this also clears `admin`; both are re-applied
     below — acceptable in the dev environment):
     ```
     dotnet ef database drop --force \
       --project src/Modules/Users/Dominodo.Users.Persistence \
       --startup-project src/Bootstrap/Dominodo.Api --context UsersDbContext
     ```
  3. Regenerate the single `InitialUsers` migration and apply it:
     ```
     dotnet ef migrations add InitialUsers \
       --project src/Modules/Users/Dominodo.Users.Persistence \
       --startup-project src/Bootstrap/Dominodo.Api --context UsersDbContext --output-dir Migrations
     dotnet ef database update \
       --project src/Modules/Users/Dominodo.Users.Persistence \
       --startup-project src/Bootstrap/Dominodo.Api --context UsersDbContext
     ```
  4. Re-apply the `Admin` schema (dropped in step 2):
     ```
     dotnet ef database update \
       --project src/Modules/Admin/Dominodo.Admin.Persistence \
       --startup-project src/Bootstrap/Dominodo.Api --context AdminDbContext
     ```

**Expected outcome:** A single clean `InitialUsers` migration; `Roles.Scope` column populated;
`PlatformRoleAssignments` table with the seeded SuperAdmin row; no `PlatformConstants` in code; login
issues `role=SuperAdmin` for the bootstrap user purely from data.

**Exit criteria:**
- `dotnet ef database update` recreates `users` (and `admin` re-applied) with the new schema + seed.
- SQL check: `Roles` has a `Scope` column (SuperAdmin=Platform, others=Tenant); `PlatformRoleAssignments`
  has exactly one seeded row; `tenants.create`/`tenants.manage` present in `Permissions` and mapped to
  SuperAdmin in `RolePermissions`.
- `POST /api/v1/auth/login` as the bootstrap SuperAdmin returns a token whose `role` claim = `SuperAdmin`,
  with **no** reference to a hardcoded user id anywhere in the codebase (`PlatformConstants` deleted).
- Build (warnings-as-errors) + architecture tests green.

---

## Phase 8 — Roles management (list / create / update) ✅ COMPLETED (2026-07-16) — runtime verified after Wolverine migration

**Runtime verification (host now boots — MassTransit→Wolverine migration done, see below):**
- Login as bootstrap SuperAdmin → access token whose `role` claim = `SuperAdmin`, **derived from the
  `PlatformRoleAssignment` row** (no hardcoded id). `GET /api/v1/roles` unauthenticated → **401**;
  authenticated → **200** (5 seeded roles, `Scope` + `permissionIds`). `GET /api/v1/permissions` → **200**
  (11 perms incl. `tenants.create`/`tenants.manage` in group `Plataforma`). `POST /api/v1/roles` → **201**;
  `PUT /api/v1/roles/{id}` (non-system) → **204**; `PUT /api/v1/roles/1` (system) → **403**; duplicate
  name → **409**; unknown permission id → **400**; `GET /api/v1/roles/{id}` → **200** with updated data.

### MassTransit → Wolverine migration (requested after the license blocker; doc 07 rewritten by the user)

- **Packages:** `Directory.Packages.props` — `MassTransit*` 9.1.2 removed; added `WolverineFx`,
  `WolverineFx.EntityFrameworkCore`, `WolverineFx.SqlServer`, `WolverineFx.RuntimeCompilation` (6.19.0).
  Core Wolverine no longer ships the Roslyn runtime compiler → RuntimeCompilation package required for
  `TypeLoadMode.Dynamic` (dev). csproj refs updated per project; `Shared.Infrastructure` lost its unused
  MassTransit refs.
- **Host (`Program.cs`):** `builder.Host.UseWolverine(...)` — `MessageStorageSchemaName="wolverine"`,
  `MultipleHandlerBehavior.Separated`, `MessageIdentity.IdAndDestination`, per-module
  `opts.AddUsersMessaging(cs)`/`AddAdminMessaging(cs)`, `opts.Discovery.AddAdminHandlers()`,
  `UseDurableLocalQueues()`. `builder.Host.UseResourceSetupOnStartup()` auto-provisions the `wolverine`
  schema tables. `ServiceLocationPolicy.AlwaysAllowed` (MediatR's `ISender` is factory-registered, so
  Wolverine's generated handler must service-locate it — one report per handler at codegen, not per message).
- **Per-module messaging helpers** (in each `*.Persistence`, where the DbContext is visible/internal):
  `AddXMessaging(this WolverineOptions, cs)` → `AddDbContextWithWolverineIntegration<TDbContext>((sp,x)=>
  UseSqlServer + interceptors)` + `PersistMessagesWithSqlServer(cs, MessageStoreRole.Ancillary).Enroll<T>()`.
  The DbContext registration moved OUT of `AddXPersistence()` (which now only registers repos + IUnitOfWork).
- **DbContexts:** dropped MassTransit `AddInboxStateEntity`/`AddOutboxMessageEntity`/`AddOutboxStateEntity`
  from both `OnModelCreating`. Migrations reset (drop DB, regenerate `InitialUsers`/`InitialAdmin`) so the
  3 MassTransit tables are gone; Wolverine's envelope tables live in the `wolverine` schema.
- **Consumer → handler:** `UserOtpRequestedConsumer : IConsumer<T>` replaced by a Wolverine convention
  handler `UserOtpRequestedHandler` (public — Wolverine's generated code can't reach internal types;
  it's an inbound adapter like a controller and only dispatches THIS module's MediatR command).
  Dependencies (`ISender`) injected as **method parameters** (constructor injection tripped
  ServiceLocationPolicy). Registered explicitly via `AddAdminHandlers()` → `IncludeType<T>()`.
- **Publish:** `RequestPhoneVerificationCommandHandler` uses `IMessageBus.PublishAsync` (was `IBus.Publish`).
- **Verified end-to-end:** `verify/request` → Wolverine persists + routes the `UserOtpRequestedIntegrationEvent`
  → Admin handler fires → `SendOtpNotificationCommand` executes (only the WhatsApp adapter HTTP call fails,
  against the stub URL `whatsapp.provider.local` — expected, no WireMock in the manual run). `/health/ready`
  → 200. Docs 01/06/11/README de-MassTransit'd; doc 07 was rewritten by the user.
- **Deviation from doc 07:** the doc's `CustomizeHandlerDiscovery(x => x.Includes.IsNotPublic())` does NOT
  enable internal handlers — Wolverine's `HandlerQuery` excludes non-public types via a separate
  `WithCondition` that the include cannot override, and generated code can't access internals anyway.
  Resolved by a public handler + explicit `IncludeType`.


**Execution notes:**
- `Role.Create(id, name, description, scope)` factory (non-system, scope immutable); `IRoleRepository`
  extended with `ExistsByNameAsync(name, excludeRoleId)`, `ListAsync(PageRequest)`, `GetMaxIdAsync`
  (new-role ids = max+1, since `Role.Id` is `ValueGeneratedNever`).
- Queries: `GetRolesQuery` → `PagedResult<RoleDto>` (incl. `Scope` string + `PermissionIds`),
  `GetRoleByIdQuery` → `RoleDto` (404 if missing), `GetPermissionsQuery` → `IReadOnlyList<PermissionDto>`
  (Contracts DTO, ordered catalog incl. `Plataforma` group).
- Commands: `CreateRoleCommand` (+validator: scope must parse to `Platform`/`Tenant`; unique name →
  `Conflict`; unknown permission ids → `Validation`), `UpdateRoleCommand` (rename + reassign perms;
  `Scope` not in command = immutable; **system roles → `Forbidden`** as the "disallowed edit" guard;
  unknown perms → `Validation`; missing role → `NotFound`).
- `RoleDto` kept internal to Application (not exposed on the facade). `PermissionDto` reused from Contracts.
- Controllers in Application: `RolesController` (`[Authorize]`; `GET /api/v1/roles`, `GET /roles/{id}`;
  `POST /roles` + `PUT /roles/{id}` with `[Authorize(Policy="SuperAdmin")]`), `PermissionsController`
  (`[Authorize]`; `GET /api/v1/permissions`). JWT bearer + the `SuperAdmin` policy (`RequireRole`) were
  already wired in `Shared.Infrastructure.AddJwtAuthentication` (Phase 6) — no host change needed.
- **Build 0 warnings/errors; arch tests 14/14 green.**

> **✅ Runtime block resolved:** the host originally could not boot because **MassTransit 9.1.2** (the
> commercial release) threw `ConfigurationException: License must be specified...`. Resolved by migrating
> the bus to **Wolverine** (ADR-0009; see the migration notes above). The host now boots and all role
> endpoints were exercised live (401/403/409/400/201/204/200 as listed under "Runtime verification").

**Objective:** RBAC catalog management for roles, with permissions exposed read-only, protected by the
SuperAdmin policy.

**Inputs / prerequisites:** Phases 2, 6, 7; docs 03, 04, 08, 11.

**Actions:**
- Queries: `GetRolesQuery` (paged `PagedResult<RoleDto>` incl. `Scope` and assigned permissions),
  `GetRoleByIdQuery`, `GetPermissionsQuery` (read-only catalog list, incl. the `Plataforma` group).
- Commands: `CreateRoleCommand` (requires a `Scope` (`Platform`|`Tenant`); unique name → Conflict;
  assign existing permission ids → invalid id `NotFound`/`Validation`), `UpdateRoleCommand` (rename +
  reassign permissions; guard `IsSystem` roles from disallowed edits; `Scope` is immutable once set).
- Authorization: `[Authorize]` + a `SuperAdmin` policy on the write endpoints. The policy checks the
  `role` claim now **derived from `PlatformRoleAssignment`** (Phase 7) — no hardcoded id. Reads require
  authentication.
- Controllers: `GET /api/v1/roles`, `GET /api/v1/roles/{id}`, `POST /api/v1/roles`,
  `PUT /api/v1/roles/{id}`, `GET /api/v1/permissions`.

**Expected outcome:** Roles can be listed, created, and updated with permission assignment and scope;
authorization enforced.

**Exit criteria:** all role endpoints behave and are authorized as specified; arch tests green.

---

## Phase 9 — Docs, ADRs, CI gate & final verification ✅ COMPLETED (2026-07-16)

**Execution notes:**
- **`docs/domain/00-domain-model.md`:** added an "Estado de implementación (MVP slice)" section marking
  `Users` (registration/OTP/login/roles) and `Admin` (OTP notifications) as implemented, documenting the
  token model (`sub` + one `role` claim per `Platform`-scope `PlatformRoleAssignment`), the facade surface
  today (`GetUserById`/`GetUserByPhone`/`GetPlatformPermissions`), and engine=SQL Server / bus=Wolverine.
  Header stays v2 (no structural change).
- **`docs/architecture/06`:** DbContext example → SQL Server + registered via
  `AddDbContextWithWolverineIntegration` (cross-ref doc 07); new subsection documenting enum storage as
  `int` (`Role.Scope`) and the `PlatformRoleAssignments` table (unique `(UserId,RoleId)`, seeded SuperAdmin).
  **`docs/architecture/11`:** health check `AddNpgSql` → `AddSqlServer`; OpenTelemetry Wolverine
  instrumentation. Docs 01/README/07 already de-MassTransit'd during the migration.
- **ADRs added:** 0006 (stack: SQL Server; JWT access+refresh; password-only login), 0007 (OTP through
  `Admin`, WhatsApp + email fallback), 0008 (RBAC: `Role.Scope` + platform authority via
  `PlatformRoleAssignment`, SuperAdmin is data not bypass), 0009 (bus: Wolverine replaces MassTransit).
  `docs/adr/README.md` index updated.
- **ArchitectureTests:** the 14 tests cover Users/Admin boundaries — cross-module only via `Contracts`
  (both directions), handlers internal, Domain/Application/Persistence/Contracts layering, modules not
  depending on concrete adapters, shared kernel/abstractions layering. All green.
- **CI:** `.github/workflows/ci.yml` (push/PR to main) → `dotnet restore` + `build -c Release`
  (warnings-as-errors gate) + `dotnet test Dominodo.ArchitectureTests -c Release`. Validated locally in
  Release: build 0 warnings, arch tests 14/14.
- **Not live-exercised** (needs a delivered OTP code / a non-SuperAdmin token; WhatsApp provider is a stub
  with no WireMock in the manual run): `verify/confirm` → Active, refresh/logout rotation, and the
  non-SuperAdmin → 403 path. These are implemented (Phases 5/6/8) and covered by handler logic + the
  authorization policy; only the live click-through is deferred.

**Objective:** Record the decisions, update affected docs, and lock enforcement in CI.

**Inputs / prerequisites:** Phases 1–8.

**Actions:**
- Update `docs/domain/00-domain-model.md` §1/§4 to mark registration/OTP/login/roles as implemented and
  note the token model (incl. `role` claims derived from `PlatformRoleAssignment`); the header note is
  already at **v2** — bump only on a further structural change.
- Update `docs/architecture/06-persistence.md` and `11-cross-cutting.md` examples from Npgsql →
  SQL Server (or add a short "engine = SQL Server" note) so docs match reality. Document how `Role.Scope`
  is stored (int vs string) and the `PlatformRoleAssignments` table.
- Add ADRs: **ADR-0006** (stack: SQL Server; JWT access+refresh; password-only login for MVP),
  **ADR-0007** (OTP delivered through the `Admin` Notifications module — WhatsApp with email fallback),
  and **ADR-0008** (RBAC: role `Scope`; platform authority via `PlatformRoleAssignment`; SuperAdmin is a
  seeded assignment, not a hardcoded bypass). Update `docs/adr/README.md` index.
- Ensure `ArchitectureTests` cover `Users` and `Admin` boundaries (only Contracts crossed; handlers
  internal; adapters referenced only by the host).
- Add a CI workflow (build + `dotnet test Dominodo.ArchitectureTests`) as a required gate.

**Expected outcome:** Docs and ADRs reflect the implementation; boundaries enforced in CI.

**Exit criteria:** all docs/ADRs updated; arch tests green locally and in CI.

---

## End-to-end verification

1. `dotnet build` (warnings-as-errors) and `dotnet test tests/Dominodo.ArchitectureTests` — arch tests green.
2. After the Phase 7 migration reset, `dotnet ef database update` for `UsersDbContext` and
   `AdminDbContext` recreates `users` and `admin` schemas with seed data — including `Roles.Scope`
   (SuperAdmin=Platform, rest=Tenant) and one seeded `PlatformRoleAssignments` row for the bootstrap
   SuperAdmin.
3. Manual/integration flow against the running host (external providers stubbed via WireMock):
   - `POST /api/v1/users` → `201` (user `PendingVerification`); duplicate phone → `409`.
   - `POST /api/v1/auth/verify/request` → OTP delivered through Admin (WhatsApp stub; email fallback when
     no WhatsApp).
   - `POST /api/v1/auth/verify/confirm` with the code → user `Active`; wrong/expired code → 400/409.
   - `POST /api/v1/auth/login` (phone+password) → access + refresh tokens; bad creds → `401`.
   - `POST /api/v1/auth/refresh` rotates; `POST /api/v1/auth/logout` revokes (reuse → `401`).
   - As bootstrap SuperAdmin (whose `role=SuperAdmin` claim comes from its `PlatformRoleAssignment`):
     `GET/POST/PUT /api/v1/roles` and `GET /api/v1/permissions` work; unauthenticated → `401`,
     non-SuperAdmin → `403`.
4. `/health/ready` returns healthy (SQL Server reachable).
