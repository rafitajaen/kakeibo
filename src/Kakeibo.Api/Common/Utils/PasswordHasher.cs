using System.Security.Cryptography;
using System.Text;

namespace Kakeibo.Api.Common.Utils;

// Hashes and verifies passwords using PBKDF2-SHA512
public static class PasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const int Iterations = 350_000; // OWASP recommended minimum
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

    // Hashes a password using PBKDF2-SHA512 with a random salt.
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        // Concatenate salt + hash into a single byte array for storage
        var result = new byte[SaltSize + KeySize];
        salt.CopyTo(result, 0);
        hash.CopyTo(result, SaltSize);

        return Convert.ToBase64String(result);
    }

    // Verifies a password against a stored hash using constant-time comparison.
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        var hashBytes = Convert.FromBase64String(hashedPassword);
        var salt = hashBytes[..SaltSize];
        var storedHash = hashBytes[SaltSize..];

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        return CryptographicOperations.FixedTimeEquals(storedHash, hash);
    }
}
