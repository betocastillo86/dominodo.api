using System.Text.Json;
using Dominodo.E2E.Clients.Core.Models;
using Dominodo.E2E.Core.Json;
using Refit;

namespace Dominodo.E2E.Clients.Core;

public static class ApiResponseExtensions
{
    /// <summary>
    /// Deserializes the RFC 9457 error body of a non-success response into a
    /// <see cref="ProblemDetailsModel"/>. Returns null if there is no error content.
    /// </summary>
    public static ProblemDetailsModel? GetProblem<T>(this ApiResponse<T> response)
    {
        var content = response.Error?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ProblemDetailsModel>(content, E2EJsonOptions.Default);
    }

    /// <summary>
    /// Same as <see cref="GetProblem{T}"/> but for a bodyless <see cref="IApiResponse"/> (endpoints whose
    /// success path returns no content, e.g. <c>202 Accepted</c> / <c>204 No Content</c>).
    /// </summary>
    public static ProblemDetailsModel? GetProblem(this IApiResponse response)
    {
        var content = response.Error?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ProblemDetailsModel>(content, E2EJsonOptions.Default);
    }
}
