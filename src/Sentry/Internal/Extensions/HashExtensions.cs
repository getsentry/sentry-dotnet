namespace Sentry.Internal.Extensions;

internal static class HashExtensions
{
    public static string GetHashString(this string str, bool upperCase = true)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        #if NET9_0 && ANDROID
        var hash = SHA1.HashData(bytes);
        return hash.ToHexString(upperCase);
        #else
        var sha = SHA1.Create();
        var hash = sha.ComputeHash(bytes);
        return hash.ToHexString(upperCase);
        #endif
    }
}
