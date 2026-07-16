namespace Dominodo.Shared.Kernel;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
