using Dominodo.Operations.Application.Abstractions;
using Dominodo.Shared.Kernel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Operations.Persistence.Sequences;

// Atomically returns the next per-tenant value for a (prefix, year) counter. The read-modify-write is a
// single UPDATE … OUTPUT under UPDLOCK/SERIALIZABLE, inserting the row on first use — so concurrent
// callers never collide. Scoped to the caller's current tenant (never crosses tenants).
internal sealed class SequenceProvider(OperationsDbContext db, ITenantContext tenant) : ISequenceProvider
{
    public async Task<int> NextAsync(string prefix, int year, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SET NOCOUNT ON;
            DECLARE @next INT;
            UPDATE operations.OperationSequence WITH (UPDLOCK, SERIALIZABLE)
                SET @next = Value = Value + 1
                WHERE TenantId = @tenantId AND Prefix = @prefix AND [Year] = @year;
            IF @next IS NULL
            BEGIN
                INSERT INTO operations.OperationSequence (TenantId, Prefix, [Year], Value)
                VALUES (@tenantId, @prefix, @year, 1);
                SET @next = 1;
            END;
            SELECT @next AS Value;
            """;

        var value = await db.Database
            .SqlQueryRaw<int>(
                sql,
                new SqlParameter("@tenantId", tenant.TenantId),
                new SqlParameter("@prefix", prefix),
                new SqlParameter("@year", year))
            .FirstAsync(cancellationToken);

        return value;
    }
}
