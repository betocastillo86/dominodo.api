using Dominodo.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace Dominodo.Shared.Infrastructure.Persistence;

// The module's unit of work under Wolverine's durable outbox. Replaces the old "the DbContext IS the
// IUnitOfWork + DispatchDomainEventsInterceptor" model: it collects the domain events raised by the
// tracked aggregates and enrolls them in the module's transactional outbox, so the aggregate mutation
// and the outbox envelopes commit in ONE transaction (SaveChangesAndFlushMessagesAsync). Wolverine's
// durability agent then delivers each event async/durable to in-module Wolverine handlers via the
// durable local queue — the immediate in-process dispatch is intentionally gone (doc 07).
//
// Registered per module in its Persistence DI, so the DbContext stays internal to the module.
public sealed class WolverineUnitOfWork<TDbContext>(IDbContextOutbox<TDbContext> outbox) : IUnitOfWork
    where TDbContext : DbContext
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var context = outbox.DbContext;

        var aggregates = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(e => e.DomainEvents).ToList();
        aggregates.ForEach(e => e.ClearDomainEvents());

        // Wolverine routes by the event's runtime type, so a concrete event held in an IDomainEvent
        // variable still reaches its own handler. No handler registered → PublishAsync is a no-op.
        foreach (var domainEvent in domainEvents)
        {
            await outbox.PublishAsync(domainEvent);
        }

        // Persists the aggregate changes AND the outbox envelopes in a single transaction, then flushes
        // the messages to the durable local queue after commit. The affected-row count is not surfaced
        // by the outbox API and is unused by the sole caller (UnitOfWorkBehavior).
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        return 0;
    }
}
