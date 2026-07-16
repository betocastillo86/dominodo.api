using System.Net.Http.Json;
using Dominodo.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Dominodo.Adapters.WhatsApp;

internal sealed class WhatsAppSender(HttpClient http, ILogger<WhatsAppSender> logger) : IWhatsAppSender
{
    public async Task SendAsync(WhatsAppMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            to = message.To,
            type = "text",
            text = new { body = message.Body }
        };

        var response = await http.PostAsJsonAsync("v1/messages", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("WhatsApp provider returned {Status} for {To}", response.StatusCode, message.To);
            throw new WhatsAppDeliveryException(response.StatusCode);
        }

        logger.LogInformation("WhatsApp message delivered to {To}", message.To);
    }
}
