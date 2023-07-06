namespace Sentry;

/// <summary>
/// Holds a fully-inclusive range of HTTP status codes.
/// e.g. Start = 500, End = 599 represents the range 500-599.
/// </summary>
public readonly record struct HttpStatusCodeRange
{
    /// <summary>
    /// The inclusive start of the range.
    /// </summary>
    public int Start { get; init; }

    /// <summary>
    /// The inclusive end of the range.
    /// </summary>
    public int End { get; init; }

    /// <summary>
    /// Creates a range that will only match a single value.
    /// </summary>
    /// <param name="statusCode">The value in the range.</param>
    public HttpStatusCodeRange(int statusCode)
    {
        Start = statusCode;
        End = statusCode;
    }

    /// <summary>
    /// Creates a range that will match all values between <paramref name="start"/> and <paramref name="end"/>.
    /// </summary>
    /// <param name="start">The inclusive start of the range.</param>
    /// <param name="end">The inclusive end of the range.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="start"/> is greater than <paramref name="end"/>.
    /// </exception>
    public HttpStatusCodeRange(int start, int end)
    {
        if (start > end)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Range start must be after range end");
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Implicitly converts a tuple of ints to a <see cref="HttpStatusCodeRange"/>.
    /// </summary>
    /// <param name="range">A tuple of ints to convert.</param>
    public static implicit operator HttpStatusCodeRange((int Start, int End) range) => new(range.Start, range.End);

    /// <summary>
    /// Implicitly converts an int to a <see cref="HttpStatusCodeRange"/>.
    /// </summary>
    /// <param name="statusCode">An int to convert.</param>
    public static implicit operator HttpStatusCodeRange(int statusCode)
    {
        return new HttpStatusCodeRange(statusCode);
    }

    /// <summary>
    /// Implicitly converts an <see cref="HttpStatusCode"/> to a <see cref="HttpStatusCodeRange"/>.
    /// </summary>
    /// <param name="statusCode">A status code to convert.</param>
    public static implicit operator HttpStatusCodeRange(HttpStatusCode statusCode)
    {
        return new HttpStatusCodeRange((int)statusCode);
    }

    /// <summary>
    /// Implicitly converts a tuple of <see cref="HttpStatusCode"/> to a <see cref="HttpStatusCodeRange"/>.
    /// </summary>
    /// <param name="range">A tuple of status codes to convert.</param>
    public static implicit operator HttpStatusCodeRange((HttpStatusCode start, HttpStatusCode end) range)
    {
        return new HttpStatusCodeRange((int)range.start, (int)range.end);
    }

    /// <summary>
    /// Checks if a given status code is contained in the range.
    /// </summary>
    /// <param name="statusCode">Status code to check.</param>
    /// <returns>True if the range contains the given status code.</returns>
    public bool Contains(int statusCode)
        => statusCode >= Start && statusCode <= End;

    /// <summary>
    /// Checks if a given status code is contained in the range.
    /// </summary>
    /// <param name="statusCode">Status code to check.</param>
    /// <returns>True if the range contains the given status code.</returns>
    public bool Contains(HttpStatusCode statusCode) => Contains((int)statusCode);
}
