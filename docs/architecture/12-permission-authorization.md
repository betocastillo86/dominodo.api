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
User ──1:N──▶ Membership (tenant-scoped, deferred) ──N:1──▶ Role (Scope=Tenant) ──▶ …
                                                             Role.Scope ∈ { Platform, Tenant }
```

- `Permission` is a **catalog entity** with a stable string `Code` (`roles.manage`), a `Description`,
  and a `Group`. It is seeded, not user-created.
- A `Role` carries `RoleScope` — `Platform` (cross-tenant authority, e.g. `SuperAdmin`) or `Tenant`
  (a role a user holds *within* one conjunto).
- `PlatformRoleAssignment` grants platform roles (no tenant). `Membership` (deferred) grants a
  tenant role for a specific `(user, tenant)`.

### Effective permissions = platform (always) ∪ tenant (when scoped)

```
GetEffectivePermissionsAsync(userId, tenantId?):
    perms  = permissions of the user's Platform-scope roles      # ALWAYS — no tenant needed
    if tenantId is present:
        perms ∪= permissions of the user's role in that tenant   # Membership (deferred)
    return perms                                                 # SuperAdmin's seeded role already holds all
```

This is the whole point-1 requirement made concrete:

- **Platform permissions never depend on a tenant.** `[HasPermission("tenants.create")]` works with no
  `X-Tenant` header, because platform-role permissions are resolved unconditionally.
- **Tenant permissions require the tenant.** `[HasPermission("requests.manage")]` only passes when the
  permission comes from the caller's Membership in the resolved `X-Tenant`.

Until the Membership slice lands, only the platform branch resolves — which is enough to protect every
platform-scoped endpoint end to end today.

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

First cut: `IMemoryCache` with a short TTL (30–60 s) — simple and correct. Hardening: the Users module
publishes an integration event when a role's permissions or a Membership change
(`RolePermissionsChangedIntegrationEvent`, `MembershipChangedIntegrationEvent`); an in-host handler
evicts the affected cache keys, giving immediate freshness with zero per-request DB cost.

## SuperAdmin — no role is ever hardcoded

Authorization is **always** by permission. There is **no role short-circuit** anywhere in the pipeline
— not in the handler, the tenant middleware, or `ITenantContext`. `SuperAdmin` is just a seeded role
that happens to carry **every** permission, so it resolves to a superset that satisfies any
`[HasPermission(...)]`. Grant or revoke its power by changing its permissions in the seed — never by
special-casing its name in code. Renaming or deleting the role changes only which permissions resolve;
it can never silently bypass a check, because no check reads a role name.

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

- Codes are lowercase, dotted `resource.action`: `users.manage`, `requests.create`, `deliveries.register`.
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
