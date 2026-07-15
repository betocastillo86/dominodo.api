# 05 — Ports & Adapters

## What it is

The domain and application layers never talk to infrastructure directly. Instead they depend on
**ports** — interfaces that express *what* the application needs ("send an email", "store a file",
"load a PQR") — and infrastructure provides **adapters** — concrete implementations that know *how*
(SMTP, blob storage, EF Core). The application depends only on the port; the composition root
(`Dominodo.Api`) picks the adapter and wires it in.

**There is no single `Infrastructure` project.** Grouping all technical concerns into one god-project
creates a dumping ground that every module ends up depending on and that becomes impossible to split.
Instead, each external dependency is its **own small adapter project**, referenced only by the
composition root, and reusable across modules.

## Why

- The application stays testable and framework-free: swap the real adapter for a fake in tests, or
  for a different implementation in production, without touching business code.
- One project per dependency keeps blast radius small and makes ownership obvious.
- Reusable adapters (email, WhatsApp, storage) are shared across modules through a shared port,
  instead of being reimplemented or hidden inside a shared monolith.
- When a module is extracted, it takes its ports with it and the new host wires whatever adapters it
  needs.

## Two kinds of ports

The rule for *where a port lives* is: **the consumer owns the port.**

### Module-owned ports

When the need is specific to one module, the port is defined **inside that module** (in `Domain` or
`Application`) and implemented by the module's **own adapter** — most commonly persistence.

```csharp
// Dominodo.Pqrs.Domain/Ports/IPqrRepository.cs
public interface IPqrRepository
{
    void Add(Pqr pqr);
    Task<Pqr?> GetByIdAsync(Guid id, CancellationToken ct);
}
```

The adapter lives in the module's `Persistence` project (see [06 — Persistence](./06-persistence.md)).

### Shared ports

When the capability is generic and reused by many modules (sending an email, pushing a notification,
storing a file), the port lives in `Dominodo.Shared.Abstractions` and the adapter is a standalone,
reusable project under `Adapters/`. A module depends on the **abstraction**, never on the adapter.

```csharp
// Dominodo.Shared.Abstractions/Notifications/IEmailSender.cs
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public sealed record EmailMessage(string To, string Subject, string HtmlBody);
```

## A full shared port → adapter example

The adapter is a typed `HttpClient` with options binding and resilience (Polly). It implements the
shared port and knows nothing about any module.

```csharp
// Dominodo.Adapters.Email/EmailSenderOptions.cs
public sealed class EmailSenderOptions
{
    public const string SectionName = "Adapters:Email";
    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
    public int TimeoutSeconds { get; init; } = 10;
}
```

```csharp
// Dominodo.Adapters.Email/EmailSender.cs
internal sealed class EmailSender(HttpClient http, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("v1/messages", message, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Email provider returned {Status}", response.StatusCode);
            throw new EmailDeliveryException(response.StatusCode);
        }
    }
}
```

```csharp
// Dominodo.Adapters.Email/DependencyInjection.cs
public static IServiceCollection AddEmailAdapter(this IServiceCollection services, IConfiguration config)
{
    services.AddOptions<EmailSenderOptions>()
        .Bind(config.GetSection(EmailSenderOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.AddHttpClient<IEmailSender, EmailSender>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<EmailSenderOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
    })
    .AddResilienceHandler("email", builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        });
        builder.AddTimeout(TimeSpan.FromSeconds(5));
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions());
    });

    return services;
}
```

The application uses the port with no knowledge of any of the above:

```csharp
internal sealed class NotifyResidentsHandler(IEmailSender email) : ICommandHandler<NotifyResidentsCommand>
{
    public async Task<Result> Handle(NotifyResidentsCommand command, CancellationToken ct)
    {
        await email.SendAsync(new EmailMessage(command.To, command.Subject, command.Body), ct);
        return Result.Success();
    }
}
```

## Composition — the only place ports meet adapters

`Dominodo.Api` is the composition root. It is the single project that references `Adapters.*` and the
module `Persistence` projects, and it wires each port to a concrete adapter.

```csharp
// Dominodo.Api/Program.cs
builder.Services
    // shared adapters
    .AddEmailAdapter(builder.Configuration)
    .AddWhatsAppAdapter(builder.Configuration)
    .AddStorageAdapter(builder.Configuration)
    .AddPushAdapter(builder.Configuration)
    // modules (each registers its own handlers, validators, persistence, facade)
    .AddPqrsModule(builder.Configuration)
    .AddTenantsModule(builder.Configuration)
    .AddPackagesModule(builder.Configuration);
```

Swapping an implementation (a different email provider, an in-memory storage for a demo) is a change
in this one file. No module changes.

## Inbound vs outbound adapters

- **Outbound (driven) adapters** — everything above: the application calls *out* through a port
  (email, storage, persistence, message bus).
- **Inbound (driving) adapters** — controllers and message consumers call *in*. They live in the
  module and translate an external request (HTTP, a bus message) into a MediatR command/query. They
  are the module's entry points; see [03 — CQRS](./03-cqrs-mediatr.md) and
  [07 — Inter-Module Communication](./07-inter-module-communication.md).

## Do / Don't

- **Do** define a port in the layer that consumes it; module-specific → in the module,
  generic/shared → in `Shared.Abstractions`.
- **Do** give each external dependency its own adapter project.
- **Do** keep resilience (retry, timeout, circuit breaker) and options binding inside the adapter.
- **Do** wire ports to adapters only in `Dominodo.Api`.
- **Don't** create a catch-all `Infrastructure` project.
- **Don't** reference an adapter from a module — reference the port.
- **Don't** leak provider types (SDK request/response classes) through a port; translate at the
  adapter boundary.
