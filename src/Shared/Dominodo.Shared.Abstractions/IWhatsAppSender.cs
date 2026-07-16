namespace Dominodo.Shared.Abstractions;

public sealed record WhatsAppMessage(string To, string Body);

public interface IWhatsAppSender
{
    Task SendAsync(WhatsAppMessage message, CancellationToken cancellationToken = default);
}
