using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dominodo.Shared.Infrastructure.Swagger;

/// <summary>
/// Applies the API-Explorer defaults to each operation so the caller never has to type the
/// <c>version</c> segment: it is bound from the selected Swagger document and removed from the
/// visible parameters. Also flags deprecated operations and fills in default parameter metadata.
/// Canonical Asp.Versioning + Swashbuckle filter.
/// </summary>
internal sealed class SwaggerDefaultValuesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        // Bind response content types from ApiExplorer.
        foreach (var responseType in apiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse
                ? "default"
                : responseType.StatusCode.ToString();

            if (!operation.Responses.TryGetValue(responseKey, out var response))
            {
                continue;
            }

            foreach (var contentType in response.Content.Keys)
            {
                if (responseType.ApiResponseFormats.All(f => f.MediaType != contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (operation.Parameters is null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Name == parameter.Name);

            if (description is null)
            {
                continue;
            }

            parameter.Description ??= description.ModelMetadata?.Description;
            parameter.Required |= description.IsRequired;
        }

        // The version path segment is bound from the chosen document — hide it from the UI.
        var versionParameter = operation.Parameters
            .FirstOrDefault(p => string.Equals(p.Name, "version", StringComparison.OrdinalIgnoreCase)
                                 && p.In == ParameterLocation.Path);

        if (versionParameter is not null)
        {
            operation.Parameters.Remove(versionParameter);
        }
    }
}
