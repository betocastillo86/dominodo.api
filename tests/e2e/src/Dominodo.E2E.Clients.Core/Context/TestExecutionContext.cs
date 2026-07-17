namespace Dominodo.E2E.Clients.Core.Context;

/// <summary>
/// Ambient per-test correlation id and test name, injected into request headers by
/// <c>CorrelationIdHandler</c> for end-to-end traceability against the API logs.
/// </summary>
public static class TestExecutionContext
{
    private static readonly AsyncLocal<string?> CorrelationIdValue = new();
    private static readonly AsyncLocal<string?> TestNameValue = new();

    public static string? CorrelationId
    {
        get => CorrelationIdValue.Value;
        set => CorrelationIdValue.Value = value;
    }

    public static string? TestName
    {
        get => TestNameValue.Value;
        set => TestNameValue.Value = value;
    }

    public static void Set(string correlationId, string testName)
    {
        CorrelationIdValue.Value = correlationId;
        TestNameValue.Value = testName;
    }

    public static void Clear()
    {
        CorrelationIdValue.Value = null;
        TestNameValue.Value = null;
    }
}
