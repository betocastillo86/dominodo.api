using Dominodo.Shared.Kernel;
using Dominodo.Users.Domain.Events;
using Dominodo.Users.Domain.ValueObjects;

namespace Dominodo.Users.Domain.Users;

public sealed class User : AggregateRoot
{
    private User() { } // EF Core

    private User(
        Guid id,
        PhoneNumber phone,
        Email? email,
        string firstName,
        string lastName,
        string? passwordHash,
        string preferredLanguage) : base(id)
    {
        Phone = phone.Value;
        Email = email?.Value;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
        PreferredLanguage = preferredLanguage;
        Status = UserStatus.PendingVerification;
    }

    public string Phone { get; private set; } = null!;
    public string? Email { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DocumentType? DocumentType { get; private set; }
    public string? DocumentNumber { get; private set; }
    public string? PasswordHash { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTimeOffset? PhoneVerifiedAtUtc { get; private set; }
    public DateTimeOffset? EmailVerifiedAtUtc { get; private set; }
    public string PreferredLanguage { get; private set; } = "es";
    public string? AvatarUrl { get; private set; }
    public string? Profile { get; private set; }

    public static Result<User> Register(
        PhoneNumber phone,
        Email? email,
        string firstName,
        string lastName,
        string? passwordHash,
        string preferredLanguage = "es")
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Error.Validation("User.FirstNameRequired", "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Error.Validation("User.LastNameRequired", "Last name is required.");
        }

        var user = new User(
            Guid.NewGuid(),
            phone,
            email,
            firstName.Trim(),
            lastName.Trim(),
            passwordHash,
            string.IsNullOrWhiteSpace(preferredLanguage) ? "es" : preferredLanguage);

        user.Raise(new UserRegisteredDomainEvent(user.Id, user.Phone, user.Email));
        return user;
    }

    public Result RequestPhoneVerification(string code, bool hasWhatsApp)
    {
        if (PhoneVerifiedAtUtc is not null)
        {
            return Error.Conflict("User.PhoneAlreadyVerified", "The phone is already verified.");
        }

        Raise(new PhoneVerificationRequestedDomainEvent(Id, Phone, Email, code, hasWhatsApp));
        return Result.Success();
    }

    public Result VerifyPhone(IClock clock)
    {
        if (PhoneVerifiedAtUtc is not null)
        {
            return Error.Conflict("User.PhoneAlreadyVerified", "The phone is already verified.");
        }

        PhoneVerifiedAtUtc = clock.UtcNow;
        Raise(new UserPhoneVerifiedDomainEvent(Id, PhoneVerifiedAtUtc.Value));
        return Result.Success();
    }

    public Result Activate()
    {
        if (Status == UserStatus.Active)
        {
            return Error.Conflict("User.AlreadyActive", "The user is already active.");
        }

        if (PhoneVerifiedAtUtc is null)
        {
            return Error.Conflict("User.PhoneNotVerified", "The phone must be verified before activation.");
        }

        Status = UserStatus.Active;
        return Result.Success();
    }

    public void SetDocument(DocumentType documentType, string documentNumber)
    {
        DocumentType = documentType;
        DocumentNumber = documentNumber;
    }
}
