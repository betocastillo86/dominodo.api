using Dominodo.Users.Domain.Authentication;
using Dominodo.Users.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Users.Persistence.Repositories;

internal sealed class VerificationCodeRepository(UsersDbContext db) : IVerificationCodeRepository
{
    public void Add(VerificationCode code) => db.VerificationCodes.Add(code);

    public Task<VerificationCode?> GetLatestActiveAsync(
        string phone,
        VerificationPurpose purpose,
        CancellationToken cancellationToken = default) =>
        db.VerificationCodes
            .Where(v => v.Phone == phone && v.Purpose == purpose && v.ConsumedAtUtc == null)
            .OrderByDescending(v => v.ExpiresAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
}
