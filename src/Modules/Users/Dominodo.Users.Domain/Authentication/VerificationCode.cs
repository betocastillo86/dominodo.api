using Dominodo.Shared.Kernel;

namespace Dominodo.Users.Domain.Authentication;

// System-level OTP record (domain-model §1.6). Stores the code HASH, never the plaintext.
public sealed class VerificationCode : AggregateRoot
{
    private VerificationCode() { } // EF Core

    private VerificationCode(
        Guid id,
        Guid? userId,
        string phone,
        VerificationPurpose purpose,
        string codeHash,
        DateTimeOffset expiresAtUtc) : base(id)
    {
        UserId = userId;
        Phone = phone;
        Purpose = purpose;
        CodeHash = codeHash;
        ExpiresAtUtc = expiresAtUtc;
        Attempts = 0;
    }

    public Guid? UserId { get; private set; }
    public string Phone { get; private set; } = null!;
    public VerificationPurpose Purpose { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }
    public int Attempts { get; private set; }

    public static VerificationCode Issue(
        Guid? userId,
        string phone,
        VerificationPurpose purpose,
        string codeHash,
        DateTimeOffset expiresAtUtc)
        => new(Guid.NewGuid(), userId, phone, purpose, codeHash, expiresAtUtc);

    public bool IsConsumed => ConsumedAtUtc is not null;

    public bool IsExpired(IClock clock) => clock.UtcNow >= ExpiresAtUtc;

    public void RegisterAttempt() => Attempts++;

    public void Consume(IClock clock) => ConsumedAtUtc = clock.UtcNow;
}
