using MediatR;

namespace Dominodo.Shared.Kernel.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
