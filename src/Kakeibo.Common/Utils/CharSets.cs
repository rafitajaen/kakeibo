namespace Kakeibo.Common.Utils;

// Character sets for random string generation
public static class CharSets
{
    public const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    public const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string Digits = "0123456789";
    public const string Alphanumeric = Lowercase + Uppercase + Digits;
    public const string AlphanumericSafe = "23456789abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ"; // No ambiguous chars (0, O, 1, I, l)
}
