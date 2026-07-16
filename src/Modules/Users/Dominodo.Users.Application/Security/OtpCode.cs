using System.Security.Cryptography;

namespace Dominodo.Users.Application.Security;

internal static class OtpCode
{
    // Cryptographically-strong numeric code of the given length (zero-padded).
    public static string Generate(int length)
    {
        var max = (int)Math.Pow(10, length);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString().PadLeft(length, '0');
    }
}
