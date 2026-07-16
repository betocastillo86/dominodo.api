namespace Dominodo.Shared.Kernel;

// Marker for a domain event. No longer a MediatR INotification: domain events are persisted to the
// module's transactional Wolverine outbox (same tx as the aggregate) and delivered async/durable to
// in-module Wolverine handlers — not dispatched in-process via MediatR. See docs/architecture/07.
public interface IDomainEvent;
