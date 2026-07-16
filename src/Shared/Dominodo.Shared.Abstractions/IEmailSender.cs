namespace Dominodo.Shared.Abstractions;

public sealed record EmailMessage(string To, string Subject, string HtmlBody, string? PlainTextBody = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
