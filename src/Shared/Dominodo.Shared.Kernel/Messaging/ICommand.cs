using MediatR;

namespace Dominodo.Shared.Kernel.Messaging;

// Marker shared by both command shapes so pipeline behaviors (e.g. UnitOfWorkBehavior) can target
// "any command" regardless of whether it returns a value.
public interface IBaseCommand;

public interface ICommand : IBaseCommand, IRequest<Result>;

public interface ICommand<TResponse> : IBaseCommand, IRequest<Result<TResponse>>;
