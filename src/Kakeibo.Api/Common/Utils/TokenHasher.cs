using System.Security.Cryptography;
using System.Text;

namespace Kakeibo.Api.Common.Utils;

// Hashes token strings with SHA-256 for secure storage.
// Tokens are generated in plain text, sent to the user, and stored only as their hash.
public static class TokenHasher
{
    // Returns a hex-encoded SHA-256 hash of the given token string.
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
