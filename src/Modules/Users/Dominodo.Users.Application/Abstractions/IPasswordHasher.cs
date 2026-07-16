namespace Dominodo.Users.Application.Abstractions;

// Application-owned port for password hashing (verify comes into play at Login, Phase 6).
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
