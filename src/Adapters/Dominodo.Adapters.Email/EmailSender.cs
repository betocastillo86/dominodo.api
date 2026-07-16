using System.Net.Http.Json;
using Dominodo.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Dominodo.Adapters.Email;

internal sealed class EmailSender(HttpClient http, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            to = message.To,
            subject = message.Subject,
            html = message.HtmlBody,
            text = message.PlainTextBody
        };

        var response = await http.PostAsJsonAsync("v1/messages", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Email provider returned {Status} for {To}", response.StatusCode, message.To);
            throw new EmailDeliveryException(response.StatusCode);
        }

        logger.LogInformation("Email delivered to {To}", message.To);
    }
}
