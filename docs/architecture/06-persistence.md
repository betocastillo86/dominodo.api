# 06 — Persistence

## What it is

Each module owns its data. There is **one physical database** for Dominodo, divided into **one schema
per module**, and **one `DbContext` per module** mapped to that schema. Modules never share tables,
never define foreign keys across schemas, and never open a transaction that spans two modules.
Persistence is implemented as the module's **own outbound adapter** (`<Module>.Persistence`) behind
the repository ports defined in the module's `Domain`.

## Why

- One schema per module is the sweet spot for a modular monolith: real logical isolation, a single
  database to operate, and a clean cut line when a module is extracted (its tables move to a new
  database as-is).
- No cross-schema foreign keys means no hidden coupling and no distributed-join temptation. Referential
  needs across modules are satisfied through the module facade or integration events, not the database.
- Per-module `DbContext` keeps migrations, mappings, and change tracking scoped to one module.

## One database, many schemas

Every module's `DbContext` uses the **same connection string** but a **different default schema**:

```csharp
// Dominodo.Pqrs.Persistence/PqrsDbContext.cs
internal sealed class PqrsDbContext(DbContextOptions<PqrsDbContext> options) : DbContext(options)
{
    public const string Schema = "pqrs";

    public DbSet<Pqr> Pqrs => Set<Pqr>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PqrsDbContext).Assembly);
        // Wolverine's durable message storage lives in the "wolverine" schema, provisioned by the bus
        // (not this module's EF migrations) — see doc 07.
    }
}
```

```
Database "dominodo"
├── schema pqrs.*        ← PqrsDbContext
├── schema tenants.*     ← TenantsDbContext
├── schema packages.*    ← PackagesDbContext
└── ...
```

## EF mappings via IEntityTypeConfiguration

One configuration class per aggregate, colocated in the module's `Persistence` project. Never map
with data annotations on domain types — the domain stays framework-free
(see [02 — DDD Building Blocks](./02-ddd-building-blocks.md)).

```csharp
// Dominodo.Pqrs.Persistence/Configurations/PqrConfiguration.cs
internal sealed class PqrConfiguration : IEntityTypeConfiguration<Pqr>
{
    public void Configure(EntityTypeBuilder<Pqr> builder)
    {
        builder.ToTable("Pqrs");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Subject).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Body).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.TenantId).IsRequired();
        builder.HasIndex(p => p.TenantId); // tenant scoping is applied explicitly — see doc 09
    }
}
```

### Table & column naming

- **Tables are `PascalCase`, pluralized** — `RefreshTokens`, `RolePermissions`, `VerificationCodes`
  (never `refresh_tokens`). Always set the name explicitly with `builder.ToTable("...")` rather than
  relying on the `DbSet` property name.
- **Columns are `PascalCase`** — the EF default from the property name; do not override to snake_case.
- **Schemas are lowercase, single word** — `users`, `tenants`, `operations`, `admin` — set once per
  `DbContext` via `modelBuilder.HasDefaultSchema(...)`.
- The per-module migrations history table stays `__ef_migrations` (EF infrastructure table, not a
  domain table).

### Enums & the `Role.Scope` / `PlatformRoleAssignments` case (Users)

- **Enums are stored as `int`** via `builder.Property(x => x.Scope).HasConversion<int>()`. `Role.Scope`
  (`RoleScope { Platform = 0, Tenant = 1 }`) is persisted as the `int` column `Scope` on `Roles`, seeded
  per role (`SuperAdmin = 0/Platform`, the rest `= 1/Tenant`).
- **`PlatformRoleAssignments`** (`Id` Guid PK, `UserId` Guid, `RoleId` int) carries platform authority
  by data: a unique index on `(UserId, RoleId)`, an index on `UserId`, and an internal FK to `Roles`
  (`OnDelete: Restrict`). The bootstrap SuperAdmin is one seeded row here — no hardcoded user-id check.
  The login token's `role` claim(s) are derived by joining these rows to `Roles` filtered by
  `Scope = Platform`.

## Repository + Unit of Work

Repositories implement the domain-owned ports. They deal in **aggregate roots only**. They do **not**
call `SaveChangesAsync` — committing is the job of the `UnitOfWorkBehavior`
(see [03 — CQRS](./03-cqrs-mediatr.md)).

```csharp
// Dominodo.Pqrs.Persistence/Repositories/PqrRepository.cs
internal sealed class PqrRepository(PqrsDbContext db) : IPqrRepository
{
    public void Add(Pqr pqr) => db.Pqrs.Add(pqr);
    public Task<Pqr?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Pqrs.FirstOrDefaultAsync(p => p.Id == id, ct);
}
```

`IUnitOfWork` is implemented by the module's `DbContext`. `SaveChangesAsync` is the single commit
point where domain events are dispatched and the outbox is flushed, all in one transaction:

```csharp
// Dominodo.Shared.Kernel
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

## Interceptors (shared plumbing)

Cross-cutting persistence behavior lives in `Shared.Infrastructure` and is attached to every module's
`DbContext`:

- **AuditableEntityInterceptor** — sets `CreatedAtUtc`/`CreatedBy` on insert and
  `UpdatedAtUtc`/`UpdatedBy` on update, using `IClock` and the current user. Aggregates never set
  these by hand.
- **DispatchDomainEventsInterceptor** — after a successful save, publishes the domain events raised by
  tracked aggregates through in-process MediatR (within the same transaction). See
  [07 — Inter-Module Communication](./07-inter-module-communication.md).

Engine = **SQL Server** (`Microsoft.EntityFrameworkCore.SqlServer`). The DbContext is registered
through Wolverine's EF integration (`AddDbContextWithWolverineIntegration<T>`, so the module's outbox
is enrolled — see [07](./07-inter-module-communication.md)); the module's own `AddXPersistence(...)`
registers only its repositories and `IUnitOfWork`. Options binding + interceptors are identical:

```csharp
// each module's messaging helper (in *.Persistence), called from the host's UseWolverine(...)
opts.Services.AddDbContextWithWolverineIntegration<PqrsDbContext>((sp, options) =>
{
    options.UseSqlServer(config.GetConnectionString("Dominodo"),
        sql => sql.MigrationsHistoryTable("__ef_migrations", PqrsDbContext.Schema));
    options.AddInterceptors(
        sp.GetRequiredService<AuditableEntityInterceptor>(),
        sp.GetRequiredService<DispatchDomainEventsInterceptor>());
});
opts.PersistMessagesWithSqlServer(cs, role: MessageStoreRole.Ancillary).Enroll<PqrsDbContext>();

// in AddPqrsPersistence(IServiceCollection):
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PqrsDbContext>());
services.AddScoped<IPqrRepository, PqrRepository>();
```

Note the **per-schema migrations history table** (`__ef_migrations` in the `pqrs` schema): each module
tracks its own migrations independently, so extracting a module carries its migration history with it.

## Migrations per module

Migrations are generated and applied **per `DbContext`**, so each module evolves its schema on its
own timeline:

```bash
# generate a migration for one module
dotnet ef migrations add AddPqrClosedAt \
  --project src/Modules/Pqrs/Dominodo.Pqrs.Persistence \
  --startup-project src/Bootstrap/Dominodo.Api \
  --context PqrsDbContext

# apply
dotnet ef database update \
  --project src/Modules/Pqrs/Dominodo.Pqrs.Persistence \
  --startup-project src/Bootstrap/Dominodo.Api \
  --context PqrsDbContext
```

## Reads

Command handlers load aggregates through repositories (tracked). Query handlers may use a read-only
context (or the same `DbContext` with `AsNoTracking()`) and project directly into DTOs — they do not
go through the aggregate. Tenant scoping is applied explicitly on every query
(see [09 — Multitenancy](./09-multitenancy.md)).

## Do / Don't

- **Do** give each module its own schema, `DbContext`, and migration history.
- **Do** keep repositories at the aggregate-root granularity.
- **Do** let `SaveChangesAsync` (via the UnitOfWork behavior) be the single commit point.
- **Do** map with `IEntityTypeConfiguration`, keeping the domain free of EF attributes.
- **Don't** create a foreign key, view, or join across module schemas.
- **Don't** call `SaveChangesAsync` inside a handler or repository.
- **Don't** reference another module's `DbContext` or entity types.
