using System.Net;

namespace Dominodo.Adapters.Email;

public sealed class EmailDeliveryException(HttpStatusCode statusCode)
    : Exception($"Email provider returned {(int)statusCode} ({statusCode}).")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
