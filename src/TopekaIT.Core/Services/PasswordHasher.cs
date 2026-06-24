using System.Security.Cryptography;

namespace TopekaIT.Core.Services;

/// <summary>
/// PBKDF2 password hashing with iteration metadata so old hashes can be upgraded after a good login.
/// </summary>
public static class PasswordHasher
{
    private const int SaltBytes = 32;
    private const int KeyBytes = 32;
    public const int LegacyIterations = 100_000;
    public const int CurrentIterations = 600_000;

    public static (string hash, string salt) Hash(string password)
    {
        var result = Hash(password, CurrentIterations);
        return (result.hash, result.salt);
    }

    public static (string hash, string salt, int iterations) HashWithMetadata(string password)
        => Hash(password, CurrentIterations);

    public static (string hash, string salt, int iterations) Hash(string password, int iterations)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, iterations, HashAlgorithmName.SHA256, KeyBytes);
        return (Convert.ToBase64String(key), Convert.ToBase64String(saltBytes), iterations);
    }

    public static bool Verify(string password, string hash, string salt)
        => Verify(password, hash, salt, LegacyIterations);

    public static bool Verify(string password, string hash, string salt, int iterations)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt)) return false;
        var saltBytes = Convert.FromBase64String(salt);
        var expected = Convert.FromBase64String(hash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, iterations, HashAlgorithmName.SHA256, KeyBytes);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
