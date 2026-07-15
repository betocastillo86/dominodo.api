# 09 — Multitenancy

## What it is

A **tenant** in Dominodo is a residential complex (*conjunto residencial*): the natural boundary that
owns apartments, residents, PQRs, packages, and everything else. Nearly all business data belongs to
exactly one tenant. We isolate tenants with **row-level `TenantId`** on tenant-owned aggregates, a
resolved **`ITenantContext`**, and **explicit, on-demand query scoping** through a single helper.

We deliberately do **not** use a global EF query filter. Super-admin dashboards need to read across
all tenants, and a global filter would force `IgnoreQueryFilters()` everywhere — turning the safe
default into constant exceptions. Instead, scoping is explicit but funneled through one mechanism so
it stays consistent and testable.

## Why

- Row-level isolation is operationally simple: one database, one schema per module, one migration path
  — no per-tenant schema/database sprawl.
- An explicit `ForCurrentTenant` call keeps cross-tenant reads (super-admin) first-class rather than a
  filter to bypass.
- Funneling all scoping through one helper avoids the failure mode of hand-written
  `.Where(x => x.TenantId == ...)` scattered across the codebase, where a single omission leaks data
  between tenants.

## TenantId on aggregates

Tenant-owned aggregates carry a `TenantId`. System-level data (the tenant registry itself, global
notification configuration, super-admin accounts) is **not** tenant-scoped and has no `TenantId`.

```csharp
public sealed class Pqr : AggregateRoot
{
    public Guid TenantId { get; private set; }
    // ...
}
```

Index `TenantId` on every tenant-owned table (see [06 — Persistence](./06-persistence.md)); every
scoped query filters on it.

## ITenantContext

Resolved once per request from the authenticated principal (a `tenant_id` claim in the JWT) and made
available through DI. It also knows whether the caller is a super-admin.

```csharp
// Dominodo.Shared.Kernel
public interface ITenantContext
{
    Guid TenantId { get; }        // throws if accessed without a tenant in scope
    bool HasTenant { get; }
    bool IsSuperAdmin { get; }
}
```

```csharp
// Dominodo.Shared.Infrastructure/Tenancy/HttpTenantContext.cs
internal sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public bool IsSuperAdmin => User?.IsInRole("SuperAdmin") ?? false;
    public bool HasTenant => Guid.TryParse(User?.FindFirstValue("tenant_id"), out _);
    public Guid TenantId => Guid.TryParse(User?.FindFirstValue("tenant_id"), out var id)
        ? id
        : throw new InvalidOperationException("No tenant in the current context.");
}
```

## The single scoping mechanism

All tenant-scoped reads go through one extension method. Writes set `TenantId` from the context when
the aggregate is created.

```csharp
// Dominodo.Shared.Infrastructure/Tenancy/TenantQueryExtensions.cs
public static class TenantQueryExtensions
{
    public static IQueryable<T> ForCurrentTenant<T>(this IQueryable<T> query, ITenantContext tenant)
        where T : class, ITenantOwned
        => query.Where(e => e.TenantId == tenant.TenantId);
}

public interface ITenantOwned { Guid TenantId { get; } }
```

Tenant-scoped read (normal user):

```csharp
var pqrs = await db.Pqrs
    .ForCurrentTenant(tenant)     // the one, explicit, consistent scoping call
    .Where(p => p.Status == PqrStatus.Open)
    .ToListAsync(ct);
```

Cross-tenant read (super-admin dashboard) — simply do not scope, guarded by authorization:

```csharp
internal sealed class GetPqrStatsAcrossTenantsQueryHandler(IPqrsReadContext db, ITenantContext tenant)
    : IQueryHandler<GetPqrStatsAcrossTenantsQuery, PqrStatsDto>
{
    public async Task<Result<PqrStatsDto>> Handle(GetPqrStatsAcrossTenantsQuery q, CancellationToken ct)
    {
        if (!tenant.IsSuperAdmin)
            return Error.Forbidden("Pqr.CrossTenantForbidden", "Only super-admins may read across tenants.");

        var stats = await db.Pqrs /* no ForCurrentTenant */ .GroupBy(p => p.TenantId) /* ... */ .ToListAsync(ct);
        return /* ... */;
    }
}
```

## Guardrails

Because scoping is explicit rather than automatic, protect against omissions:

- **Endpoint authorization.** Cross-tenant endpoints require the `SuperAdmin` policy; regular
  endpoints require a resolved tenant. A missing tenant on a tenant-scoped endpoint is a `401/403`,
  not a silent all-tenant read.
- **Architecture / analyzer guard.** An architecture test (or Roslyn analyzer) flags raw
  `.Where(x => x.TenantId == ...)` outside the `TenantQueryExtensions` helper, so all scoping stays
  funneled through the one method (see [10 — Testing](./10-testing.md)).
- **Review checklist.** Any query over an `ITenantOwned` type either calls `ForCurrentTenant` or is an
  explicitly authorized super-admin path — no third option.

## Do / Don't

- **Do** put `TenantId` on every tenant-owned aggregate and index it.
- **Do** scope reads with `ForCurrentTenant(tenant)` — the single approved mechanism.
- **Do** guard cross-tenant reads with a super-admin authorization check.
- **Do** set `TenantId` from `ITenantContext` when creating a tenant-owned aggregate.
- **Don't** hand-roll `.Where(x => x.TenantId == ...)` in handlers.
- **Don't** rely on a global query filter (super-admin reads make it a liability here).
- **Don't** let a tenant-scoped query run without either scoping it or authorizing a cross-tenant read.
