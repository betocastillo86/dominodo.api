using Dominodo.Adapters.Email;
using Dominodo.Adapters.WhatsApp;
using Dominodo.Admin.Application;
using Dominodo.Admin.Persistence;
using Dominodo.Api;
using Dominodo.Shared.Infrastructure;
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
    // Module controllers live in each module's Application assembly — register them as parts.
    .AddApplicationPart(Dominodo.Users.Application.DependencyInjection.ApplicationAssembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("Dominodo")!,
        name: "sql-server",
        tags: ["ready"]);

var app = builder.Build();

app.UseSharedInfrastructure();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("IntegrationTests"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.MapHealthChecks("/health/live",  new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });

app.Run();

public partial class Program : IApiMarker;
