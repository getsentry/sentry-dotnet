using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Provides a pattern that can be used to match against other strings as either a substring or regular expression.
/// </summary>
[TypeConverter(typeof(SubstringOrRegexPatternTypeConverter))]
public class SubstringOrRegexPattern
{
    private readonly Regex? _regex;
    private readonly string? _substring;
    private readonly StringComparison _stringComparison;

    /// <summary>
    /// Constructs a <see cref="SubstringOrRegexPattern"/> instance.
    /// </summary>
    /// <param name="substringOrRegexPattern">The substring or regular expression pattern to match on.</param>
    /// <param name="comparison">The string comparison type to use when matching.</param>
    public SubstringOrRegexPattern(
        string substringOrRegexPattern,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        _substring = substringOrRegexPattern;
        _stringComparison = comparison;
        _regex = TryParseRegex(substringOrRegexPattern, comparison);
    }

    /// <summary>
    /// Constructs a <see cref="SubstringOrRegexPattern"/> instance.
    /// </summary>
    /// <param name="regex"></param>
    /// <remarks>
    /// Use this constructor when you need to control the regular expression matching options.
    /// We recommend setting at least <see cref="RegexOptions.Compiled"/> for performance, and
    /// <see cref="RegexOptions.CultureInvariant"/> (unless you have culture-specific matching needs).
    /// The <see cref="SubstringOrRegexPattern(string, StringComparison)"/> constructor sets these by default.
    /// </remarks>
    public SubstringOrRegexPattern(Regex regex) => _regex = regex;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="SubstringOrRegexPattern"/>.
    /// </summary>
    /// <param name="substringOrRegexPattern"></param>
    public static implicit operator SubstringOrRegexPattern(string substringOrRegexPattern)
    {
        return new SubstringOrRegexPattern(substringOrRegexPattern);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Regex"/> to a <see cref="SubstringOrRegexPattern"/>.
    /// </summary>
    /// <param name="regex"></param>
    public static implicit operator SubstringOrRegexPattern(Regex regex)
    {
        return new SubstringOrRegexPattern(regex);
    }

    /// <inheritdoc />
    public override string ToString() => _substring ?? _regex?.ToString() ?? "";

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return
            (obj is SubstringOrRegexPattern pattern)
            && pattern.ToString() == ToString();
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    internal Regex? Regex => _regex;

    internal bool IsMatch(string str) =>
        _substring == ".*" || // perf shortcut
        (_substring != null && str.Contains(_substring, _stringComparison)) ||
        _regex?.IsMatch(str) == true;

    private static Regex? TryParseRegex(string pattern, StringComparison comparison)
    {
        try
        {
            var regexOptions = RegexOptions.Compiled;

            if (comparison is
                StringComparison.InvariantCulture or
                StringComparison.InvariantCultureIgnoreCase or
                StringComparison.Ordinal or
                StringComparison.OrdinalIgnoreCase)
            {
                regexOptions |= RegexOptions.CultureInvariant;
            }

            if (comparison is
                StringComparison.CurrentCultureIgnoreCase or
                StringComparison.InvariantCultureIgnoreCase or
                StringComparison.OrdinalIgnoreCase)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            return new Regex(pattern, regexOptions);
        }
        catch
        {
            // not a valid regex
            return null;
        }
    }
}

internal static class SubstringOrRegexPatternExtensions
{
    public static bool ContainsMatch(this IEnumerable<SubstringOrRegexPattern> targets, string str) =>
        targets.Any(t => t.IsMatch(str));

    /// <summary>
    /// During configuration binding, .NET 6 and lower used to just call Add on the existing item.
    /// .NET 7 changed this to call the setter with an array that already starts with the old value.
    /// We have to handle both cases.
    /// </summary>
    /// <typeparam name="T">The List Type</typeparam>
    /// <param name="value">The set of values to be assigned</param>
    /// <returns>A IList of type T that will be consistent even if it has been set via Config</returns>
    public static IList<T> WithConfigBinding<T>(this IList<T> value)
        where T : SubstringOrRegexPattern
    {
        switch (value.Count)
        {
            case 1 when value[0].ToString() == ".*":
                // There's only one item in the list, and it's the wildcard, so reset to the initial state.
                return new AutoClearingList<T>(value, clearOnNextAdd: true);

            case > 1:
                // There's more than one item in the list.  Remove the wildcard.
                var targets = value.ToList();
                targets.RemoveAll(t => t.ToString() == ".*");
                return targets;

            default:
                return value;
        }
    }
}

internal class SubstringOrRegexPatternTypeConverter : TypeConverter
{
    // This class allows the TracePropagationTargets option to be set from config, such as appSettings.json

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        new SubstringOrRegexPattern((string)value);
}
