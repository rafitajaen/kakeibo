namespace Kakeibo.Api.Common.Utils;

// Generates unique usernames in the format "user_{7 lowercase alphanumeric chars}".
public static class UsernameGenerator
{
    private const string Prefix = "user_";
    private const int SuffixLength = 7;
    private const string SuffixCharset = CharSets.Lowercase + CharSets.Digits;

    public static string Generate()
    {
        return Prefix + RandomString.Generate(SuffixLength, SuffixCharset);
    }
}
