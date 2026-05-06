using System.Security.Cryptography;

namespace TopekaIT.Core.Services;

public static class PasswordHasher
{
    private const int SaltBytes = 32;
    private const int KeyBytes = 32;
    private const int Iterations = 100_000;

    public static (string hash, string salt) Hash(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeyBytes);
        return (Convert.ToBase64String(key), Convert.ToBase64String(saltBytes));
    }

    public static bool Verify(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt)) return false;
        var saltBytes = Convert.FromBase64String(salt);
        var expected = Convert.FromBase64String(hash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeyBytes);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
