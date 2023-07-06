namespace Sentry.Internal.Extensions;

internal static class HashExtensions
{
    public static string GetHashString(this string str, bool upperCase = true)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        using var sha = SHA1.Create();
        var hash = sha.ComputeHash(bytes);
        return hash.ToHexString(upperCase);
    }
}
