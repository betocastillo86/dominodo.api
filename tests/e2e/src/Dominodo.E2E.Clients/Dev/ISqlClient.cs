using Refit;

namespace Dominodo.E2E.Clients.Dev;

/// <summary>
/// Refit client for the host's development-only raw-SQL endpoint (<c>/api/v1/dev/sql</c>, returns 404
/// outside Development). This is the ONLY way E2E tests arrange DB state that no public endpoint exposes —
/// tests must never drive the suite to add bespoke helper endpoints in <c>src/</c>.
/// </summary>
public interface ISqlClient
{
    [Post("/api/v1/dev/sql")]
    Task<IApiResponse> Execute([Body] SqlRequestModel model);

    [Post("/api/v1/dev/sql/query")]
    Task<ApiResponse<SqlQueryResultModel>> Query([Body] SqlRequestModel model);
}

/// <summary>Body for the dev SQL endpoints: a single raw statement.</summary>
public sealed record SqlRequestModel(string Query);

/// <summary>Shape returned by <c>/api/v1/dev/sql/query</c>: the rows as column→value maps.</summary>
public sealed record SqlQueryResultModel(IReadOnlyList<Dictionary<string, object?>> Result);
