using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.Authentication;

// System-level refresh token (domain-model §1.6). Stores the token HASH; supports rotation + revocation.
public sealed class RefreshToken : AggregateRoot
{
    private RefreshToken() { } // EF Core

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        string? createdByIp) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public string? CreatedByIp { get; private set; }

    public static RefreshToken Issue(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        string? createdByIp = null)
        => new(Guid.NewGuid(), userId, tokenHash, expiresAtUtc, createdByIp);

    public bool IsActive(IClock clock) => RevokedAtUtc is null && clock.UtcNow < ExpiresAtUtc;

    public void Revoke(IClock clock, Guid? replacedByTokenId = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = clock.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
