# 09 — Multitenancy

## What it is

A **tenant** is a residential complex (*conjunto residencial*): the boundary that owns apartments,
residents, PQRs, packages, etc. We isolate tenants with **row-level `TenantId`** on tenant-owned
aggregates, a resolved **`ITenantContext`**, and **explicit query scoping** through one helper.

The tenant is resolved from an **`X-Tenant` header carrying the site slug** (e.g. `los-almendros`), which
the backend maps to a `TenantId`. The JWT does **not** decide the tenant — when present, it only
*validates* that the caller belongs to the resolved tenant. Why the slug and not the JWT:

- **The slug is what the frontend already has** (from the URL/subdomain) — no extra call to discover a `Guid`.
- **Anonymous endpoints still get scoped** (public PQR form, visitor pre-registration) — a JWT-only scheme can't.
- **Super-admins** target a tenant by setting the header, or omit it to read across all tenants.

The header name lives in `Multitenancy/TenantHeaders.Name` (single source of truth, shared by the
resolution middleware and Swagger) and is surfaced as an optional per-request parameter in Swagger —
see `docs/architecture/11-cross-cutting.md`.

We do **not** use a global EF query filter: super-admin reads across tenants would turn it into
constant `IgnoreQueryFilters()` calls. Scoping stays explicit but funneled through one mechanism.

## TenantId on aggregates

Tenant-owned aggregates carry the stable `Guid` `TenantId` (never the slug — slugs can be renamed).
System-level data (the tenant registry, global config, super-admins) has no `TenantId`.

```csharp
public sealed class Pqr : AggregateRoot
{
    public Guid TenantId { get; private set; }
}
```

Index `TenantId` on every tenant-owned table (see [06 — Persistence](./06-persistence.md)).

## Resolving the tenant

```
X-Tenant: los-almendros          Authorization: Bearer <jwt tenant_id claim> (optional)
        │
UseAuthentication  →  UseTenantResolution  →  UseAuthorization
                          │
                          slug ──(cached lookup)──▶ TenantId, then reconcile vs. JWT
```

- The **slug** decides the tenant; the middleware resolves it once and `ITenantContext.TenantId`
  returns that `Guid`.
- The **JWT** `tenant_id` claim (for non-super-admins) must match the resolved tenant, else `403`.
- An **unknown slug** is rejected `400`; an **anonymous** request carries only the slug (nothing to reconcile).

| Caller                       | JWT `tenant_id` | `X-Tenant` slug | Result                                    |
| ---------------------------- | --------------- | --------------- | ----------------------------------------- |
| Regular user, own site       | `A`             | slug of `A`     | ✅ `TenantId == A`                         |
| Regular user, forged/missing | `A`             | slug of `B` / — | ❌ `403 Tenant.Mismatch`                   |
| Unknown slug                 | *(any)*         | `nope`          | ❌ `400 Tenant.Unknown`                    |
| Anonymous (public endpoint)  | *(none)*        | slug of `A`     | ✅ `TenantId == A`, no reconciliation      |
| Super-admin, one tenant      | *(any)*         | slug of `A`     | ✅ acts on `A`                             |
| Super-admin, cross-tenant    | *(any)*         | *(absent)*      | ✅ `HasTenant == false`, reads all tenants |

> The slug **identifies** a tenant; it never **authorizes**. Authorization comes from the JWT and
> endpoint policies. Anonymous endpoints must only expose data safe to be public per tenant. The
> reconciliation check guarantees a forged slug can't let an *authenticated* caller escape their tenant.

## The slug → TenantId resolver

The tenant registry lives in the **Tenants module**, but the resolution middleware lives in
`Shared.Infrastructure`, which may not reference a module's `Contracts`. So the lookup is a **port in
`Shared.Abstractions`, implemented by the Tenants module**, wired in the host:

```csharp
// Shared.Abstractions — the port
public interface ITenantDirectory
{
    Task<Guid?> ResolveSlugAsync(string slug, CancellationToken ct);   // null if no such site
}

// Tenants.Application — internal impl, cached (slug→id is ~static; invalidate on rename)
internal sealed class TenantDirectory(...) : ITenantDirectory { /* cache + registry lookup */ }
```

## ITenantContext

`TenantId` is the resolved `Guid` (stashed in `HttpContext.Items` by the middleware);
`HttpTenantContext` reads it synchronously and stays free of any module dependency.

```csharp
// Shared.Kernel
public interface ITenantContext
{
    Guid TenantId { get; }   // throws if no tenant resolved this request
    bool HasTenant { get; }
    bool IsSuperAdmin { get; }
}
```

## The resolution middleware

Runs **after** authentication, **before** authorization. It resolves the slug and enforces the one
invariant that makes a client-supplied header safe: *an authenticated caller only acts on their own tenant.*

```csharp
public async Task Invoke(HttpContext ctx, ITenantDirectory directory)   // directory injected per-request
{
    var slug = ctx.Request.Headers["X-Tenant"].ToString();
    var isTenantUser = (ctx.User.Identity?.IsAuthenticated ?? false) && !ctx.User.IsInRole("SuperAdmin");

    if (!string.IsNullOrWhiteSpace(slug))
    {
        var tenantId = await directory.ResolveSlugAsync(slug, ctx.RequestAborted);
        if (tenantId is null) return Reject(ctx, 400, "Tenant.Unknown");

        ctx.Items["TenantId"] = tenantId.Value;

        // super-admins exempt; a tenant-bound token must agree with the resolved tenant
        if (isTenantUser && Guid.TryParse(ctx.User.FindFirstValue("tenant_id"), out var claim)
            && claim != tenantId.Value)
            return Reject(ctx, 403, "Tenant.Mismatch");
    }
    else if (isTenantUser && ctx.User.FindFirstValue("tenant_id") is not null)
    {
        return Reject(ctx, 403, "Tenant.Mismatch");   // tenant user with no site header
    }

    await next(ctx);
}
```

```csharp
app.UseAuthentication();
app.UseTenantResolution();   // ← slug → id, then reconcile vs. JWT
app.UseAuthorization();
```

## Endpoints — declare the tenant expectation explicitly

```csharp
[Authorize]                     // tenant-scoped: slug required + validated against JWT
[AllowAnonymous]                // public form: slug required, no JWT to validate
[Authorize(Policy = "SuperAdmin")]  // cross-tenant: slug optional, plus a runtime IsSuperAdmin check
```

## The single scoping mechanism

All tenant-scoped reads go through one extension method; writes set `TenantId` from the context.

```csharp
public static IQueryable<T> ForCurrentTenant<T>(this IQueryable<T> query, ITenantContext tenant)
    where T : class, ITenantOwned
    => query.Where(e => e.TenantId == tenant.TenantId);

public interface ITenantOwned { Guid TenantId { get; } }
```

```csharp
var pqrs = await db.Pqrs.ForCurrentTenant(tenant).Where(p => p.Status == PqrStatus.Open).ToListAsync(ct);
```

Cross-tenant read (super-admin) — do not scope, guard with authorization:

```csharp
if (!tenant.IsSuperAdmin)
    return Error.Forbidden("Pqr.CrossTenantForbidden", "Only super-admins may read across tenants.");
var stats = await db.Pqrs.GroupBy(p => p.TenantId) /* ... */ .ToListAsync(ct);
```

## Guardrails

- **Middleware:** unknown slug → `400`; resolved tenant ≠ JWT (or missing) for a tenant user → `403`.
- **Endpoints:** every endpoint is explicitly `[Authorize]`, `[AllowAnonymous]`, or `SuperAdmin`-guarded.
  A tenant-scoped handler reaching `TenantId` with no resolved slug throws — never a silent all-tenant read.
- **Analyzer/architecture test:** flags raw `.Where(x => x.TenantId == ...)` outside `ForCurrentTenant`
  (see [10 — Testing](./10-testing.md)).

## Do / Don't

- **Do** send the site **slug** in `X-Tenant`; resolve it via `ITenantDirectory` (Tenants-owned, cached).
- **Do** validate the resolved tenant against the JWT's `tenant_id` (reject on mismatch).
- **Do** scope reads with `ForCurrentTenant`; set `TenantId` from `ITenantContext` on create.
- **Do** guard cross-tenant reads with an `IsSuperAdmin` check.
- **Don't** resolve the tenant from the JWT claim — the claim only *validates* the slug.
- **Don't** reference the Tenants module's `Contracts` from `Shared.Infrastructure` — use the port.
- **Don't** trust the slug for authority, or expose non-public data through `[AllowAnonymous]`.
- **Don't** hand-roll `.Where(x => x.TenantId == ...)`, or rely on a global query filter.
