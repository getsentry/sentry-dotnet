namespace Sentry.Internal.Extensions;

internal static class StringExtensions
{
    // Used to convert enum value into snake case, which is how Sentry represents them
    public static string ToSnakeCase(this string str) =>
        Regex.Replace(str, @"(\p{Ll})(\p{Lu})", "$1_$2").ToLowerInvariant();

    /// <summary>
    /// Returns <c>null</c> if <paramref name="str"/> is <c>null</c> or contains only whitespace.
    /// Otherwise, returns <paramref name="str"/>.
    /// </summary>
    public static string? NullIfWhitespace(this string? str) => string.IsNullOrWhiteSpace(str) ? null : str;

    public static long ParseHexAsLong(this string str)
    {
        // It should be in hex format, such as "0x7fff5bf346c0"
        if (str.StartsWith("0x") &&
            long.TryParse(str[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        else if (long.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }
        throw new FormatException($"ParseHexAsLong() cannot parse '{str}'");
    }
}
