using Bogus;

namespace Dominodo.E2E.Clients.Common;

public abstract class BaseRequestBuilder
{
    protected Faker Faker { get; } = new("en");
}
