namespace Sentry;

/// <summary>
/// Extension methods for collections of <see cref="HttpStatusCodeRange"/>.
/// </summary>
internal static class HttpStatusCodeRangeExtensions
{
    /// <summary>
    /// Checks if any range in the collection contains the given status code.
    /// </summary>
    /// <param name="ranges">Collection of ranges to check.</param>
    /// <param name="statusCode">Status code to check.</param>
    /// <returns>True if any range contains the given status code.</returns>
    internal static bool ContainsStatusCode(this IEnumerable<HttpStatusCodeRange> ranges, int statusCode)
        => ranges.Any(range => range.Contains(statusCode));

    /// <summary>
    /// Checks if any range in the collection contains the given status code.
    /// </summary>
    /// <param name="ranges">Collection of ranges to check.</param>
    /// <param name="statusCode">Status code to check.</param>
    /// <returns>True if any range contains the given status code.</returns>
    internal static bool ContainsStatusCode(this IEnumerable<HttpStatusCodeRange> ranges, HttpStatusCode statusCode)
        => ranges.ContainsStatusCode((int)statusCode);
}
