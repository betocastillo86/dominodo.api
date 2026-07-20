using Dominodo.Adapters.Email;
using Dominodo.Adapters.WhatsApp;
using Dominodo.Admin.Application;
using Dominodo.Admin.Persistence;
using Dominodo.Api;
using Dominodo.Shared.Application;
using Dominodo.Shared.Infrastructure;
using Dominodo.Shared.Infrastructure.Swagger;
using Dominodo.Tenants.Application;
using Dominodo.Tenants.Persistence;
using Dominodo.Users.Application;
using Dominodo.Users.Persistence;
using JasperFx.CodeGeneration.Model;
using JasperFx.Resources;
using Serilog;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext());

builder.Services.AddSharedApplication();
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddDominodoTelemetry("dominodo-api");

// Outbound adapters (wired only in the composition root)
builder.Services.AddWhatsAppAdapter(builder.Configuration);
builder.Services.AddEmailAdapter(builder.Configuration);

// Modules
builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddUsersPersistence();
builder.Services.AddAdminModule(builder.Configuration);
builder.Services.AddAdminPersistence();
builder.Services.AddTenantsModule(builder.Configuration);
builder.Services.AddTenantsPersistence();

// Permission resolution port (doc 12) — implemented here because it bridges to the Users facade,
// which Shared.Infrastructure may not reference. Cached per (userId, tenant) with a short TTL.
builder.Services.AddMemoryCache();
builder.Services.AddScoped<Dominodo.Shared.Abstractions.IPermissionProvider, Dominodo.Api.Auth.CachingPermissionProvider>();

// Message bus (Wolverine, MIT — doc 07). Durable local queues are the in-process transport today;
// swapping to RabbitMQ / Azure Service Bus is config only. Each module enrolls its own DbContext +
// ancillary SQL Server message store (its own schema); the shared envelope storage lives in "wolverine".
var wolverineConnectionString = builder.Configuration.GetConnectionString("Dominodo")!;
builder.Host.UseWolverine(opts =>
{
    opts.Durability.MessageStorageSchemaName = "wolverine";
    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;   // each module: own tx + retry
    opts.Durability.MessageIdentity = MessageIdentity.IdAndDestination; // same event, many modules

    // Single-instance deployment: skip Wolverine's distributed node coordination (leader election,
    // node registration, cross-node health checks over the DB control channel). Balanced (the default)
    // is for multi-replica clusters; with one instance it only produces "duplicate leader agent" /
    // split-brain warnings + StopRemoteAgent timeouts when a process dies without a clean shutdown and
    // leaves an orphaned row in wolverine.wolverine_nodes. Solo sidesteps all of that.
    // ⚠️ REMINDER: the moment we run MORE THAN ONE instance/replica, switch this back to
    // DurabilityMode.Balanced (or remove the line) — Solo assumes it is the only node and will NOT
    // coordinate agent ownership across nodes.
    opts.Durability.Mode = DurabilityMode.Solo;

    // MediatR stays for in-module dispatch (doc 07); its ISender is registered via a factory, so
    // Wolverine's generated handler code must service-locate it. Allow it (Wolverine 6 defaults to
    // NotAllowed). The report fires once per handler at codegen time, not per message.
    opts.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;

    // one enrolled DbContext + ancillary message store per module (keeps each DbContext internal)
    opts.AddUsersMessaging(wolverineConnectionString);
    opts.AddAdminMessaging(wolverineConnectionString);
    opts.AddTenantsMessaging(wolverineConnectionString);

    // module message handlers are internal; register them explicitly (conventional discovery skips
    // non-public types). IncludeType bypasses that filter.
    opts.Discovery.AddAdminHandlers();
    opts.Discovery.AddTenantsHandlers();
    opts.Discovery.AddUsersHandlers();

    // Host-side consumer: evicts the permission-cache entry on any membership change (doc 12).
    opts.Discovery.IncludeType<Dominodo.Api.Auth.WhenMembershipChanged_InvalidatePermissionCache>();

    opts.Policies.UseDurableLocalQueues(); // swap to opts.UseRabbitMq(...) later — handlers unchanged
});

// Auto-provision Wolverine's message storage tables on startup (dev/pre-prod). Idempotent.
builder.Host.UseResourceSetupOnStartup();

builder.Services
    .AddControllers()
    // Module controllers live in each module's *.Api assembly — register them as parts.
    // (Admin.Api added here once it exposes controllers.)
    .AddApplicationPart(typeof(Dominodo.Users.Api.IUsersApiMarker).Assembly)
    .AddApplicationPart(typeof(Dominodo.Tenants.Api.ITenantsApiMarker).Assembly);
builder.Services.AddDominodoSwagger();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("Dominodo")!,
        name: "sql-server",
        tags: ["ready"]);

var app = builder.Build();

app.UseSharedInfrastructure();

// Dev convenience: ensure the local DB container is up + migrated before serving (idempotent, F5-friendly).
// Also runs for IntegrationTests, which shares the local database and seeds test fixtures below.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("IntegrationTests"))
{
    await app.EnsureLocalDatabaseAsync();
}

// IntegrationTests-only: seed a Platform role + user + assignment per permission (fixed ids) so tests can
// authenticate as a user carrying exactly one permission. Runtime-gated (never baked into migrations).
if (app.Environment.IsEnvironment("IntegrationTests"))
{
    await app.Services.SeedIntegrationTestDataAsync();
}

// Swagger is exposed only outside production (requirement #1). Never register it unconditionally.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("IntegrationTests"))
{
    app.UseDominodoSwagger();
}

app.MapControllers();

app.MapHealthChecks("/health/live",  new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });

app.Run();

public partial class Program : IApiMarker;
