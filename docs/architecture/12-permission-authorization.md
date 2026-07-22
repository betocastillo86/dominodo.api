# 12 — Permission-based Authorization

## What it is

Endpoints are protected by **permission**, never by role. A permission (`roles.manage`,
`requests.manage`, …) is a fine-grained capability that may live in **many** roles at once; a role is
just a named bundle of permissions. Authorizing on the permission — not the role that happens to carry
it today — means a role can be renamed, split, or re-composed without touching a single controller.

Two decisions define the model:

1. **Permissions are resolved server-side, per request — not baked into the JWT.** The token stays
   minimal (`sub`, `jti`). See [Why permissions are not in the JWT](#why-permissions-are-not-in-the-jwt).
2. **The effective permission set is a function of `(user, tenant)`**, because the same user can hold
   different roles in different tenants (via *Membership*). The token is **tenant-agnostic**; the
   tenant comes from the `X-Tenant` slug per request (see [09 — Multitenancy](./09-multitenancy.md)).

## The permission model

The catalog and role mapping already exist in the **Users** module:

```
User ──1:N──▶ PlatformRoleAssignment ──N:1──▶ Role ──1:N──▶ RolePermission ──N:1──▶ Permission
User ──1:N──▶ Membership (tenant-scoped) ──N:1──▶ Role (Scope=Tenant) ──▶ …
                                                  Role.Scope ∈ { Platform, Tenant }
```

- `Permission` is a **catalog entity** with a stable string `Code` (`roles.manage`), a `Description`,
  and a `Group`. It is seeded, not user-created.
- A `Role` carries `RoleScope` — `Platform` (cross-tenant authority, e.g. `SuperAdmin`) or `Tenant`
  (a role a user holds *within* one conjunto).
- `PlatformRoleAssignment` grants platform roles (no tenant). `Membership` grants a
  tenant role for a specific `(user, tenant)`.

### Effective permissions = platform (always) ∪ tenant (when scoped)

```
GetEffectivePermissionsAsync(userId, tenantId?):
    perms  = permissions of the user's Platform-scope roles      # ALWAYS — no tenant needed
    if tenantId is present:
        perms ∪= permissions of the user's ACTIVE-Membership role in that tenant  # Invited/Suspended grant nothing
    return perms                                                 # SuperAdmin's seeded role already holds all
```

This is the whole point-1 requirement made concrete:

- **Platform permissions never depend on a tenant.** `[HasPermission("tenants.create")]` works with no
  `X-Tenant` header, because platform-role permissions are resolved unconditionally.
- **Tenant permissions require the tenant.** `[HasPermission("requests.manage")]` only passes when the
  permission comes from the caller's Membership in the resolved `X-Tenant`.

Both branches are **active**: the tenant branch resolves through `IUsersModuleApi.GetEffectivePermissionsAsync`
(platform ∪ the user's Active-membership role in the resolved tenant), so `[HasPermission("memberships.manage")]`
passes for an `Administrador` with an `Active` membership in that conjunto, and for `SuperAdmin` everywhere via
its platform grant.

## Why permissions are not in the JWT

Putting permissions in the token is tempting but wrong here, for three compounding reasons:

- **The token is tenant-agnostic, the permissions are not.** A single JWT is used across every tenant
  the user belongs to (the tenant is chosen per request by `X-Tenant`). There is no single "the
  permissions" to bake in — the effective set is `f(user, tenant)`, decided at request time.
- **Size.** Embedding every permission of every tenant a user belongs to (8 conjuntos × N permisos)
  bloats the token and ships on every request, including anonymous-adjacent ones.
- **Freshness.** A permission revoked mid-session would stay valid until the token expires. Server-side
  resolution revokes immediately (or within a short cache TTL).

The trade-off — one lookup per authorized request — is neutralized by a two-layer cache (below).

## Where each piece lives

`Shared.Infrastructure` may **not** reference a module's `Contracts`, so resolution is a **port** in
`Shared.Abstractions`, implemented in the host — the same shape as `ITenantDirectory` in
[09 — Multitenancy](./09-multitenancy.md).

| Piece | Project | Notes |
| --- | --- | --- |
| `Permissions` code constants | `Shared.Kernel` | Single source of truth for the string codes; the Users seed references these — no magic strings |
| `IPermissionProvider` (port) | `Shared.Abstractions` | `Task<PermissionSet> GetEffectivePermissionsAsync(userId, tenantId?, ct)` |
| `HasPermissionAttribute` | `Shared.Infrastructure` | `AuthorizeAttribute` with `Policy = "perm:" + code` |
| `PermissionRequirement` + `PermissionAuthorizationHandler` | `Shared.Infrastructure` | Handler depends on the **port** only + `ITenantContext` |
| `PermissionPolicyProvider` | `Shared.Infrastructure` | `IAuthorizationPolicyProvider` — builds `perm:*` policies on the fly |
| `CachingPermissionProvider` | `Dominodo.Api` (host) | Implements the port by calling `IUsersModuleApi` + `IMemoryCache` |
| `GetEffectivePermissionsAsync` | `Users` (`IUsersModuleApi` in `Contracts` + internal impl) | The actual role→permission resolution, Users-owned |

The controller keeps a controller in `<Module>.Api`, `Shared.Infrastructure` keeps the HTTP plumbing,
and `Users` keeps ownership of who-has-what — no boundary is crossed.

## The request flow

```
UseAuthentication  →  UseTenantResolution  →  UseAuthorization
   (sub from JWT)       (X-Tenant slug → TenantId       │
                         in HttpContext.Items)          ▼
                                        [HasPermission("roles.manage")]
                                        PermissionPolicyProvider → policy "perm:roles.manage"
                                        PermissionAuthorizationHandler:
                                          userId  = sub claim
                                          tenantId = ITenantContext (if HasTenant)
                                          perms    = IPermissionProvider.GetEffectivePermissionsAsync(userId, tenantId)
                                          succeed iff "roles.manage" ∈ perms  (or caller is SuperAdmin)
```

Authorization runs **after** tenant resolution, so `ITenantContext` is populated by the time the handler
needs it — no ordering hazard.

## Declaring the requirement

```csharp
[HttpPost]
[HasPermission(Permissions.RolesManage)]        // was [Authorize(Policy = "SuperAdmin")]
public async Task<IResult> Create(CreateRoleRequest request, CancellationToken ct) { ... }
```

`HasPermissionAttribute` is a thin `AuthorizeAttribute` whose `Policy` encodes the code (`perm:roles.manage`).
`PermissionPolicyProvider` recognizes the `perm:` prefix and returns a policy carrying a single
`PermissionRequirement(code)` — so there is **no per-permission policy registration** to maintain.

## Caching and invalidation

Resolution is split so the hot path stays cheap:

- **`role → permissions`** — a small, near-static map (few roles). Cached process-wide; loaded lazily
  and invalidated when a role's permissions change.
- **`(userId, tenantId) → roles`** — the per-user, per-tenant slice. Cached under `perm:{userId}:{tenantId}`
  with a short TTL.

`IMemoryCache` with a short TTL (60 s) backs the per-`(userId, tenantId)` slice. **Cache eviction on
membership change is implemented:** the Users module publishes `MembershipCreatedIntegrationEvent` /
`MembershipSuspendedIntegrationEvent` / `MembershipChangedIntegrationEvent` (role change / reactivation)
via its Wolverine outbox, and an in-host handler (`WhenMembershipChanged_InvalidatePermissionCache`)
evicts the exact `perm:{userId}:{tenantId}` key (each event carries `UserId` + `TenantId`), giving
immediate freshness (~1–2 s) with zero per-request DB cost — no longer relying on the TTL alone. Role-
permission-change eviction (`RolePermissionsChangedIntegrationEvent`) remains a future addition.

## SuperAdmin — no role is ever hardcoded

Authorization is **always** by permission. There is **no role short-circuit** anywhere in the pipeline
— not in the handler, the tenant middleware, or `ITenantContext`. `SuperAdmin` is just a seeded role
that happens to carry **every** permission, so it resolves to a superset that satisfies any
`[HasPermission(...)]`. Grant or revoke its power by changing its permissions in the seed — never by
special-casing its name in code. Renaming or deleting the role changes only which permissions resolve;
it can never silently bypass a check, because no check reads a role name.

## Testing seams — IntegrationTests seeding

Because authorization resolves the caller's permissions **from the DB by the token's `sub`** (never a
role claim), a test can only exercise a `[HasPermission(code)]` endpoint if the DB actually holds a
`User` → `PlatformRoleAssignment` → `Role` → that permission. To make this turnkey, running the API
under the **`IntegrationTests`** environment seeds — at startup, at runtime, never via EF `HasData` —
one **Platform-scope** `Role` + `User` + `PlatformRoleAssignment` per catalog permission, with **fixed
deterministic ids**:

- `RoleId = 1000 + permissionId`; `UserId = 00000000-0000-0000-0000-0000000010NN` (`NN` = permission id);
  role name is the PascalCase of the code (`roles.manage` → `RolesManage`).
- Source of truth: `Dominodo.Users.Persistence/Seed/IntegrationTestSeedData.cs`, invoked by
  `IServiceProvider.SeedIntegrationTestDataAsync()` from `Program.cs` (idempotent). The E2E project
  mirrors the ids in `DominodoConstants.IntegrationSeed` (black-box — cannot reference Persistence).
- Only **Platform** roles are seeded here (the turnkey path for `[HasPermission]` IntegrationTests).
  Tenant (Membership) scenarios are set up explicitly from the E2E tests — invite → accept → act with an
  `X-Tenant` header — now that the Membership slice has landed.
- This environment shares the Development database and uses the `dominodo-tests` JWT settings; the E2E
  token minter must match them.

## Resource-based (ownership) authorization

`[HasPermission]` is **resource-blind by design**: it answers `(user, tenant) → bool`, never
`(user, tenant, resourceId) → bool`. That is correct for capability checks ("may this user manage
apartments at all?"), but it cannot express **ownership** ("may this user read *this specific*
apartment because they live in it?"). Granting a resident `apartments.view` would leak *every*
apartment in the conjunto; withholding it blocks them from their own. This is a distinct concern.

**`IResourceAccessAuthorizer`** (port in `Shared.Abstractions`, impl `ResourceAccessAuthorizer` in
`Shared.Infrastructure`) resolves the rule **"caller holds the permission (RBAC) OR the caller owns
this resource"**:

```csharp
var allowed = await authorizer.HasAccessAsync(
    Permissions.ApartmentsView,                                  // RBAC path (staff)
    userId => apartment.Residents.Any(r => r.UserId == userId && r.IsActive),  // ownership path
    ct);
if (!allowed) { return Error.NotFound("Apartment.NotFound", "Apartment not found."); }  // leak-safe
```

- **Ownership is always a same-module data check** the caller supplies as a delegate — never a
  cross-module port. The guard stays transport- and domain-agnostic; each module decides what
  "owns" means from its own already-loaded aggregate (here, an active `ApartmentResident` row).
- The delegate runs **only when the permission is absent** (short-circuits the ownership query for
  staff who already pass RBAC). Fails closed (`false`) when there is no authenticated caller.
- The guard returns a **plain access boolean**; the handler shapes the transport error. For reads,
  denial returns the **same `NotFound`** as a missing row — **leak-safe**, no existence disclosure
  (an outsider cannot distinguish "exists but not yours" from "does not exist").
- It extends to **writes** with no redesign: swap the permission argument (e.g. `ApartmentsEdit`)
  at the top of a future edit handler; the ownership axis is identical.

**`ICurrentUser`** (interface in `Shared.Kernel`, impl `HttpCurrentUser` in `Shared.Infrastructure`)
is the caller-identity seam — a mirror of `ITenantContext`. It exposes the authenticated caller's
`UserId` (and `IsAuthenticated`) ambiently, reading the `sub`/`NameIdentifier` claim the same way the
permission handler does, so handlers and the guard need not thread the claim through every command.

| Piece | Project | Notes |
| --- | --- | --- |
| `ICurrentUser` | `Shared.Kernel` | Ambient caller identity; mirror of `ITenantContext` |
| `HttpCurrentUser` | `Shared.Infrastructure` | Reads `NameIdentifier ?? sub`; fails closed |
| `IResourceAccessAuthorizer` (port) | `Shared.Abstractions` | "permission OR caller-supplied ownership" |
| `ResourceAccessAuthorizer` | `Shared.Infrastructure` | Depends on `ICurrentUser` + `ITenantContext` + `IPermissionProvider` |

See [ADR-0010](../adr/0010-autorizacion-basada-en-recurso-propiedad.md).

## Relationship to multitenancy (revises doc 09)

[09 — Multitenancy](./09-multitenancy.md) originally described validating a JWT `tenant_id` claim against
the resolved slug. That assumes **one tenant per token**, which is incompatible with Membership (a user
belongs to *many* tenants). The revised rule:

- The token carries **no** `tenant_id`. It is tenant-agnostic.
- An authenticated caller is allowed to act on the resolved `X-Tenant` **iff they have a Membership in
  it**, or hold a **platform-scoped permission** granting cross-tenant access. That check replaces the
  old `tenant_id`-claim reconciliation and is the same lookup that feeds permission resolution — never
  a role name. Cross-tenant reads are likewise gated by a permission plus `HasTenant`, not `IsSuperAdmin`.

## Permission catalog & naming

- Codes are lowercase, dotted `resource.action`: `users.manage`, `requests.view`, `deliveries.create`.
- The catalog is seeded in Users; **every code has a matching constant in `Permissions`** — controllers
  reference the constant, never a literal.
- New permission → add to the Users seed **and** the `Permissions` constants in the same change.

## Do / Don't

- **Do** authorize with `[HasPermission(Permissions.X)]` on the controller action.
- **Do** resolve effective permissions server-side through `IPermissionProvider`.
- **Do** treat platform permissions as tenant-independent; require a tenant only for tenant-scoped ones.
- **Do** reference `Permissions.*` constants — never a raw `"roles.manage"` string.
- **Don't** put permissions (or a single `tenant_id`) in the JWT.
- **Don't** authorize on a role name — no `RequireRole`, no `IsInRole`, no `SuperAdmin` special-case. Ever.
- **Don't** reference `Users.Contracts` from `Shared.Infrastructure` — depend on the `IPermissionProvider` port.
- **Don't** validate tenant access via a JWT claim — validate Membership (see the section above).

## Guardrails

- **Handler:** a missing/blank `sub`, or a required tenant permission with no resolved tenant, fails
  closed (`403`) — never a silent allow.
- **Policy provider:** an unknown `perm:` code yields a requirement that no permission satisfies → `403`.
- **Consistency:** a seeded permission code with no `Permissions` constant (or vice-versa) is a review
  smell; keep them in lockstep.
