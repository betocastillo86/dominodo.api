using MediatR;

namespace Dominodo.Shared.Kernel.Messaging;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
