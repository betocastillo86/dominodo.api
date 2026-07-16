using Dominodo.Users.Domain.Authentication;

namespace Dominodo.Users.Domain.Ports;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
