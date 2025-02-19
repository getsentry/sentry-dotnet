namespace Sentry.Internal;

/// <summary>
/// FNV is a non-cryptographic hash.
///
/// See https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function#FNV_hash_parameters
/// </summary>
/// <remarks>
/// We use a struct to avoid heap allocations.
/// </remarks>
internal struct FnvHash
{
    public FnvHash()
    {
    }

    private const int Offset = unchecked((int)2166136261);
    private const int Prime = 16777619;

    private int HashCode { get; set; } = Offset;

    private void Combine(byte data)
    {
        unchecked
        {
            HashCode ^= data;
            HashCode *= Prime;
        }
    }

    private static int ComputeHash(byte[] data)
    {
        var result = new FnvHash();
        foreach (var b in data)
        {
            result.Combine(b);
        }

        return result.HashCode;
    }

    public static int ComputeHash(string data) => ComputeHash(Encoding.UTF8.GetBytes(data));
}
