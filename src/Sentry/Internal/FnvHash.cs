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
    // Provide a constructor that takes a parameter (so it’s not the default one).
    // This lets you explicitly set your initial hash code:
    public FnvHash(int _)
    {
        _hashCode = Offset;
    }

    private const int Offset = unchecked((int)2166136261);
    private const int Prime = 16777619;

    // Field (no initializer here—set it in the constructor)
    private int _hashCode;

    private void Combine(byte data)
    {
        unchecked
        {
            _hashCode ^= data;
            _hashCode *= Prime;
        }
    }

    private static int ComputeHashInternal(byte[] data)
    {
        // Create FnvHash via the constructor, which initializes _hashCode to Offset
        var result = new FnvHash(0);
        foreach (var b in data)
        {
            result.Combine(b);
        }
        return result._hashCode;
    }

    public static int ComputeHash(string data)
        => ComputeHashInternal(Encoding.UTF8.GetBytes(data));
}
