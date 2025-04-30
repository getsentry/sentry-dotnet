namespace Sentry.Internal;

/// <summary>
/// FNV is a non-cryptographic hash.
///
/// See https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function#FNV_hash_parameters
/// </summary>
internal static class FnvHash
{
    private const int Offset = unchecked((int)2166136261);
    private const int Prime = 16777619;

    internal static int ComputeHash(string input)
    {
        var hashCode = Offset;
        foreach (var b in Encoding.UTF8.GetBytes(input))
        {
            unchecked
            {
                hashCode ^= b;
                hashCode *= Prime;
            }
        }

        return hashCode;
    }
}
