using Dominodo.Adapters.Email;
using Dominodo.Adapters.WhatsApp;
using Dominodo.Admin.Application;
using Dominodo.Admin.Persistence;
using Dominodo.Api;
using Dominodo.Shared.Application;
using Dominodo.Shared.Infrastructure;
using Dominodo.Shared.Infrastructure.Swagger;
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

// Message bus (Wolverine, MIT — doc 07). Durable local queues are the in-process transport today;
// swapping to RabbitMQ / Azure Service Bus is config only. Each module enrolls its own DbContext +
// ancillary SQL Server message store (its own schema); the shared envelope storage lives in "wolverine".
var wolverineConnectionString = builder.Configuration.GetConnectionString("Dominodo")!;
builder.Host.UseWolverine(opts =>
{
    opts.Durability.MessageStorageSchemaName = "wolverine";
    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;   // each module: own tx + retry
    opts.Durability.MessageIdentity = MessageIdentity.IdAndDestination; // same event, many modules

    // MediatR stays for in-module dispatch (doc 07); its ISender is registered via a factory, so
    // Wolverine's generated handler code must service-locate it. Allow it (Wolverine 6 defaults to
    // NotAllowed). The report fires once per handler at codegen time, not per message.
    opts.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;

    // one enrolled DbContext + ancillary message store per module (keeps each DbContext internal)
    opts.AddUsersMessaging(wolverineConnectionString);
    opts.AddAdminMessaging(wolverineConnectionString);

    // module message handlers are internal; register them explicitly (conventional discovery skips
    // non-public types). IncludeType bypasses that filter.
    opts.Discovery.AddAdminHandlers();

    opts.Policies.UseDurableLocalQueues(); // swap to opts.UseRabbitMq(...) later — handlers unchanged
});

// Auto-provision Wolverine's message storage tables on startup (dev/pre-prod). Idempotent.
builder.Host.UseResourceSetupOnStartup();

builder.Services
    .AddControllers()
    // Module controllers live in each module's *.Api assembly — register them as parts.
    // (Admin.Api added here once it exposes controllers.)
    .AddApplicationPart(typeof(Dominodo.Users.Api.IUsersApiMarker).Assembly);
builder.Services.AddDominodoSwagger();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("Dominodo")!,
        name: "sql-server",
        tags: ["ready"]);

var app = builder.Build();

app.UseSharedInfrastructure();

// Dev convenience: ensure the local DB container is up + migrated before serving (idempotent, F5-friendly).
if (app.Environment.IsDevelopment())
{
    await app.EnsureLocalDatabaseAsync();
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
