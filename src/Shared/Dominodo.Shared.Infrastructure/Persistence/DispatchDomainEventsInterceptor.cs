using Dominodo.Shared.Kernel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Dominodo.Shared.Infrastructure.Persistence;

public sealed class DispatchDomainEventsInterceptor(IServiceScopeFactory scopeFactory) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        var entities = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();

        entities.ForEach(e => e.ClearDomainEvents());

        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
