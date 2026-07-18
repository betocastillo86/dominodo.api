using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Dominodo.Api.Controllers;

/// <summary>
/// Raw-SQL escape hatch for E2E test setup/teardown. Every action returns 404 unless the host runs in
/// Development or IntegrationTests — never exposed in staging or production. This is deliberately the
/// ONLY test-support surface: E2E tests arrange state through here instead of the suite growing bespoke
/// "dev" endpoints in the modules (which would leak test concerns into production code).
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/dev/sql")]
public sealed class SqlController(
    IConfiguration configuration,
    IWebHostEnvironment env,
    ILogger<SqlController> logger) : ControllerBase
{
    /// <summary>Executes a non-query statement (INSERT/UPDATE/DELETE/DDL). Returns rows affected.</summary>
    [HttpPost]
    [EndpointSummary("Executes raw SQL (Development/IntegrationTests only, E2E test helper). Returns rows affected.")]
    public async Task<IActionResult> Execute([FromBody] SqlRequest request, CancellationToken ct)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        logger.LogInformation("Dev SQL execute: {Query}", request.Query);

        await using var connection = new SqlConnection(configuration.GetConnectionString("Dominodo"));
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(request.Query, connection);
        var affected = await command.ExecuteNonQueryAsync(ct);

        return Ok(new { affected });
    }

    /// <summary>Runs a query (SELECT) and returns the rows as a list of column→value maps.</summary>
    [HttpPost("query")]
    [EndpointSummary("Runs a raw SQL query (Development/IntegrationTests only, E2E test helper). Returns the rows.")]
    public async Task<IActionResult> Query([FromBody] SqlRequest request, CancellationToken ct)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        logger.LogInformation("Dev SQL query: {Query}", request.Query);

        await using var connection = new SqlConnection(configuration.GetConnectionString("Dominodo"));
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(request.Query, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>(reader.FieldCount);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, ct) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return Ok(new { result = rows });
    }

    // Enabled only in Development and IntegrationTests — never in staging/production.
    private bool IsEnabled()
    {
        return env.IsDevelopment() || env.IsEnvironment("IntegrationTests");
    }
}

/// <summary>Body for the dev SQL endpoints: a single raw statement.</summary>
public sealed record SqlRequest(string Query);
