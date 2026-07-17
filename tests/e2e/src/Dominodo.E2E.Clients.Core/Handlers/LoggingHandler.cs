using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dominodo.E2E.Clients.Core.Handlers;

/// <summary>
/// Structured request/response logging. The message templates and property names are replicated
/// VERBATIM from the Pollaya E2E LoggingHandler because an external troubleshooting tool parses
/// these exact log lines. Do not change the templates, prefixes, or property names.
/// </summary>
public class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;

    private bool onlyLogErrors = false;

    public LoggingHandler(
        ILogger<LoggingHandler> logger,
        IConfiguration configuration)
    {
        this._logger = logger;
        onlyLogErrors = configuration.GetValue<bool>("ApiSettings:OnlyLogErrors");
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestBodyContent = request.Content != null ? (await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)) : string.Empty;

        var response = await base.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (onlyLogErrors && response.IsSuccessStatusCode)
        {
            return response;
        }

        _logger.LogInformation("[LoggingHandler] Before Request: {Method} {Url} RequestHeaders: {AllRequestHeaders} RequestBodyContent:{RequestBodyContent}",
            request.Method,
            request.RequestUri,
            request.Headers.ToString(),
            requestBodyContent);

        _logger.LogInformation("[LoggingHandler] Response: {StatusCode} ResponseHeaders: {AllResponseHeaders} ResponseBodyContent:{ResponseBodyContent}",
            response.StatusCode,
            response.Headers.ToString(),
            await response.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

        return response;
    }
}
