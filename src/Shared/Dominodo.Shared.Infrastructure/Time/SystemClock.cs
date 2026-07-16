using Dominodo.Shared.Kernel;

namespace Dominodo.Shared.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
