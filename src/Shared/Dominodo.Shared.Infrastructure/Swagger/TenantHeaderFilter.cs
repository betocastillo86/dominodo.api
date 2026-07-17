using Dominodo.Shared.Infrastructure.Multitenancy;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dominodo.Shared.Infrastructure.Swagger;

/// <summary>
/// Surfaces the tenant/site header (<see cref="TenantHeaders.Name"/>) as an optional per-request
/// parameter on every operation, so it can be supplied from the Swagger UI.
/// </summary>
internal sealed class TenantHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        if (operation.Parameters.Any(p => p.Name == TenantHeaders.Name && p.In == ParameterLocation.Header))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = TenantHeaders.Name,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Tenant/site slug. Required for authenticated tenant (non-SuperAdmin) users.",
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}
