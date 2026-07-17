using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dominodo.Shared.Infrastructure.Swagger;

/// <summary>
/// Adds the standard <c>401</c>/<c>403</c> ProblemDetails responses to every operation that
/// requires authorization, so controllers only need to declare their business-specific codes.
/// </summary>
internal sealed class AuthResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        var requiresAuth = metadata.OfType<IAuthorizeData>().Any();

        if (allowsAnonymous || !requiresAuth)
        {
            return;
        }

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
    }
}
