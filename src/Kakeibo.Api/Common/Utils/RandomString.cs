using System.Security.Cryptography;

namespace Kakeibo.Api.Common.Utils;

// Cryptographically secure random string generator
public static class RandomString
{
    public static string Generate(int length, string charset = CharSets.AlphanumericSafe)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        var result = new char[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = charset[bytes[i] % charset.Length];
        }

        return new string(result);
    }
}
