using Dominodo.Users.Domain.Authentication;

namespace Dominodo.Users.Domain.Ports;

public interface IVerificationCodeRepository
{
    void Add(VerificationCode code);
    Task<VerificationCode?> GetLatestActiveAsync(
        string phone,
        VerificationPurpose purpose,
        CancellationToken cancellationToken = default);
}
