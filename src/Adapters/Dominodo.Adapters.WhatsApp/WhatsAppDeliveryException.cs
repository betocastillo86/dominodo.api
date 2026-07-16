using System.Net;

namespace Dominodo.Adapters.WhatsApp;

public sealed class WhatsAppDeliveryException(HttpStatusCode statusCode)
    : Exception($"WhatsApp provider returned {(int)statusCode} ({statusCode}).")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
