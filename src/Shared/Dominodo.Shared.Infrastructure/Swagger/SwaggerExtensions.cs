using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Dominodo.Shared.Infrastructure.Swagger;

/// Composition helpers for the versioned, secured Swagger setup.
/// Documentation is driven purely by attributes on the controllers
/// (<c>[EndpointSummary]</c>, <c>[ProducesResponseType]</c>) — no XML doc comments.
public static class SwaggerExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddDominodoSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        // One SwaggerDoc per API version (see ConfigureSwaggerOptions).
        services.ConfigureOptions<ConfigureSwaggerOptions>();

        services.AddSwaggerGen(options =>
        {
            options.SupportNonNullableReferenceTypes();
            options.DescribeAllParametersInCamelCase();
            options.CustomOperationIds(api => api.ActionDescriptor.RouteValues["action"] is { } action
                ? $"{api.ActionDescriptor.RouteValues["controller"]}_{action}"
                : null);

            options.OperationFilter<SwaggerDefaultValuesFilter>();
            options.OperationFilter<TenantHeaderFilter>();
            options.OperationFilter<AuthResponsesOperationFilter>();

            // Paste-a-JWT authentication: the token is sent as `Authorization: Bearer <jwt>`.
            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste a JWT access token (without the `Bearer ` prefix)."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = BearerScheme
                    }
                }] = []
            });

        });

        return services;
    }

    public static WebApplication UseDominodoSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            // One endpoint per version → the UI shows a dropdown of all versions.
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }

            options.ConfigObject.AdditionalItems["persistAuthorization"] = true;
            options.DisplayRequestDuration();
            options.EnableTryItOutByDefault();
            options.DefaultModelsExpandDepth(-1);
        });

        return app;
    }
}
