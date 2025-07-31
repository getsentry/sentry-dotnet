// ReSharper disable CheckNamespace
namespace System.Collections.Immutable;

internal static class ImmutableCollectionsPolyfill
{
#if !NET8_0_OR_GREATER
    internal static ImmutableArray<T> DrainToImmutable<T>(this ImmutableArray<T>.Builder builder)
    {
        if (builder.Capacity == builder.Count)
        {
            return builder.MoveToImmutable();
        }

        var result = builder.ToImmutable();
        builder.Count = 0;
        builder.Capacity = 0;
        return result;
    }
#endif
}
