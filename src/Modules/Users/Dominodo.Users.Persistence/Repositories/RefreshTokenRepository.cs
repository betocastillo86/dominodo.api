using Dominodo.Users.Domain.Authentication;
using Dominodo.Users.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dominodo.Users.Persistence.Repositories;

internal sealed class RefreshTokenRepository(UsersDbContext db) : IRefreshTokenRepository
{
    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
}
