using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dominodo.Shared.Infrastructure.Swagger;

/// <summary>
/// Generates one Swagger document per discovered API version. Adding a new
/// <c>[ApiVersion("N")]</c> surfaces a new document (and UI dropdown entry) automatically —
/// no changes here are required.
/// </summary>
internal sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureNamedOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfo(description));
        }
    }

    public void Configure(string? name, SwaggerGenOptions options)
    {
        Configure(options);
    }

    private static OpenApiInfo CreateInfo(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "Dominodo API",
            Version = description.ApiVersion.ToString(),
            Description = "HTTP surface for the Dominodo modular monolith.",
            Contact = new OpenApiContact { Name = "Dominodo" }
        };

        if (description.IsDeprecated)
        {
            info.Description += " This API version has been deprecated.";
        }

        return info;
    }
}
