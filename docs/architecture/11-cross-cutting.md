# 11 — Cross-Cutting Concerns

## What it is

Concerns that apply across every module and are wired once in `Shared.Infrastructure` and the host:
correlation/tracing, idempotency, pagination, API versioning, health checks, telemetry, structured
logging, and feature flags. Modules consume them; they do not reimplement them.

## Why

- Consistency: one way to paginate, one way to version, one way to trace a request everywhere.
- Operability: correlation ids and telemetry make a single deployable observable, and keep it
  observable after modules are extracted.
- Safety: idempotency protects against duplicate writes from retries (HTTP clients and at-least-once
  event delivery — see [07 — Inter-Module Communication](./07-inter-module-communication.md)).

## Correlation id & distributed tracing

Every request carries a correlation id: read from the `X-Correlation-Id` header if present, otherwise
generated. It is attached to the logging scope, propagated on outbound HTTP calls (via a
`DelegatingHandler` in the adapters), and carried on published integration events so a flow can be
followed end to end. OpenTelemetry `ActivitySource` spans wrap requests, handlers, DB calls, and bus
publish/consume.

```csharp
// Shared.Infrastructure — ambient accessor used by logging, adapters, and the bus
public interface ICorrelationContext { string CorrelationId { get; } }
```

```csharp
// middleware sets it per request
app.Use(async (ctx, next) =>
{
    var id = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    ctx.Response.Headers["X-Correlation-Id"] = id;
    using (LogContext.PushProperty("CorrelationId", id))
        await next();
});
```

## Idempotency

Any operation that can be safely retried must be safe to run twice.

- **Inbound HTTP writes** that require idempotency accept an `Idempotency-Key` header. The key +
  response is recorded; a repeat with the same key returns the stored result instead of acting again.
- **Event consumers** dedupe by event id (or a natural key) — Wolverine delivers at least once, so a
  consumer that creates or charges something must guard against reprocessing (Wolverine's durable inbox
  plus a natural key / unique constraint).

```csharp
internal sealed class RegisterPackageConsumer(ISender sender) : IConsumer<VisitorArrivedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<VisitorArrivedIntegrationEvent> ctx)
    {
        // handler is idempotent: upsert keyed by ctx.Message.EventId / natural key
        await sender.Send(new RegisterPackageCommand(ctx.Message.PackageId));
    }
}
```

## Pagination

List endpoints return a uniform `PagedResult<T>`; never return unbounded collections. A shared
request type and helper keep paging identical across modules.

```csharp
// Shared.Kernel
public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public int Skip => (Math.Max(Page, 1) - 1) * Take;
    public int Take => Math.Clamp(PageSize, 1, 100);
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

```csharp
public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
    this IQueryable<T> query, PageRequest page, CancellationToken ct)
{
    var total = await query.LongCountAsync(ct);
    var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(ct);
    return new PagedResult<T>(items, page.Page, page.Take, total);
}
```

## API versioning

Endpoints are versioned from day one via the URL segment (`/api/v{version}/...`) using
`Asp.Versioning`. New breaking shapes add a version; existing versions stay stable.

```csharp
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
}).AddApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true; // renders /api/v1/... instead of asking for a "version" param
});
```

## API documentation (Swagger / OpenAPI)

Swagger is wired in `Shared.Infrastructure` via `AddDominodoSwagger(...)` / `UseDominodoSwagger()`
(`Swagger/SwaggerExtensions.cs`) and is **only exposed outside production** — the host registers the
middleware behind `if (env.IsDevelopment() || env.IsEnvironment("IntegrationTests"))`. Key pieces:

- **One document per API version.** `ConfigureSwaggerOptions` loops `IApiVersionDescriptionProvider`
  and emits a `SwaggerDoc` per version; `UseSwaggerUI` adds a dropdown endpoint per version. 
  `SwaggerDefaultValuesFilter`, the `version` route token never appears as a manual parameter.
- **Auth.** A `Bearer` (http/JWT) security definition lets you paste an access token into *Authorize*;
  it is sent as `Authorization: Bearer <jwt>`.
- **Tenant/site header.** `TenantHeaderFilter` surfaces the `X-Tenant` header
  (`Multitenancy/TenantHeaders.Name`) as an optional per-request parameter on every operation.
- **Response docs (attributes only, no XML comments).** Controllers declare
  `[Produces("application/json")]`, per-status `[ProducesResponseType(typeof(...), ...)]` (errors typed
  as `ProblemDetails`), and `[EndpointSummary("...")]` for the operation summary. There is **no** XML
  doc file: documentation is driven purely by attributes, so `GenerateDocumentationFile` stays off.

## Health checks

The host exposes liveness and readiness endpoints. Readiness aggregates each module's dependencies
(its database) and external adapters where a probe is meaningful.

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "sql-server", tags: ["ready"])
    .AddCheck<BusHealthCheck>("bus");

app.MapHealthChecks("/health/live",  new() { Predicate = _ => false });          // process is up
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Count == 0 }); // dependencies ok
```

## Telemetry & structured logging

- **Logging**: structured (Serilog or `Microsoft.Extensions.Logging` with JSON), always including
  `CorrelationId`, `TenantId` (when present), and request name. No string-concatenated messages —
  log with message templates and named properties.
- **Metrics & traces**: OpenTelemetry exports traces and metrics (ASP.NET Core, `HttpClient`, EF Core,
  Wolverine instrumentation) to the configured OTLP endpoint.
- These are registered once in a `AddDominodoTelemetry()` extension in `Shared.Infrastructure` and are
  identical across modules, so observability survives a module's extraction into its own service.

## Feature flags (optional)

Where a capability needs to be toggled at runtime (gradual rollout, kill switch), gate it behind a
feature-flag provider abstraction rather than configuration reads scattered in handlers.

```csharp
public interface IFeatureFlags { Task<bool> IsEnabledAsync(string flag, CancellationToken ct); }
```

Check the flag at the edge of a use case (handler entry or endpoint), keep the domain flag-free, and
remove the flag once the rollout completes.

## Do / Don't

- **Do** propagate the correlation id across HTTP calls and published events.
- **Do** make retryable writes and every event consumer idempotent.
- **Do** return `PagedResult<T>` from list endpoints with a clamped page size.
- **Do** version endpoints from the first release and register telemetry once.
- **Don't** return unbounded lists.
- **Don't** read feature flags or trace state deep inside the domain.
- **Don't** log unstructured strings or sensitive data (tokens, personal data).
