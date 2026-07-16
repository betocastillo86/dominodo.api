using Dominodo.Shared.Kernel;
using Dominodo.Shared.Kernel.Messaging;
using MediatR;

namespace Dominodo.Shared.Infrastructure.Behaviors;

internal sealed class UnitOfWorkBehavior<TRequest, TResponse>(IEnumerable<IUnitOfWork> unitsOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Commands (with or without a return value) are wrapped; queries are not.
        if (request is not IBaseCommand)
        {
            return await next();
        }

        // The command was handled → persist its work. An exception (a true abort) propagates past this
        // point, so no commit happens. An expected-failure Result is a normal outcome, not a rollback
        // signal, so it still commits any state the handler DELIBERATELY recorded (e.g. a failed OTP
        // attempt). Convention: handlers guard-first, mutate-last, and throw to abort.
        var response = await next();

        // Each module registers its own IUnitOfWork (its DbContext). A command only mutates its own
        // module's context; the others have no tracked changes and their SaveChanges is a no-op. This
        // keeps modules in SEPARATE transactions — never a shared one — regardless of registration order.
        foreach (var unitOfWork in unitsOfWork)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }

        return response;
    }
}
