using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Stores either a plain string or a Regular Expression, typically to match against filters in the SentryOptions
/// </summary>
[TypeConverter(typeof(StringOrRegexTypeConverter))]
public class StringOrRegex
{
    internal readonly Regex? _regex;
    internal readonly string? _string;

    /// <summary>
    /// Constructs a <see cref="StringOrRegex"/> instance.
    /// </summary>
    /// <param name="stringOrRegex">The prefix or regular expression pattern to match on.</param>
    public StringOrRegex(string stringOrRegex)
    {
        _string = stringOrRegex;
    }

    /// <summary>
    /// Constructs a <see cref="StringOrRegex"/> instance.
    /// </summary>
    /// <param name="regex"></param>
    /// <remarks>
    /// Use this constructor when you want the match to be performed using a regular expression.
    /// </remarks>
    public StringOrRegex(Regex regex) => _regex = regex;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="StringOrRegex"/>.
    /// </summary>
    /// <param name="stringOrRegex"></param>
    public static implicit operator StringOrRegex(string stringOrRegex)
    {
        return new StringOrRegex(stringOrRegex);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Regex"/> to a <see cref="StringOrRegex"/>.
    /// </summary>
    /// <param name="regex"></param>
    public static implicit operator StringOrRegex(Regex regex)
    {
        return new StringOrRegex(regex);
    }

    /// <inheritdoc />
    public override string ToString() => _string ?? _regex?.ToString() ?? "";

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return
            (obj is StringOrRegex pattern)
            && pattern.ToString() == ToString();
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}

internal static class StringOrRegexExtensions
{
    public static bool MatchesAny(this string parameter, IEnumerable<StringOrRegex>? patterns, IStringOrRegexMatcher matcher)
    {
        if (patterns is null)
        {
            return false;
        }
        foreach (var stringOrRegex in patterns)
        {
            if (matcher.IsMatch(stringOrRegex, parameter))
            {
                return true;
            }
        }
        return false;
    }

    public static bool MatchesSubstringOrRegex(this IEnumerable<StringOrRegex>? patterns, string parameter)
        => parameter.MatchesAny(patterns, SubstringOrPatternMatcher.Default);

    /// <summary>
    /// During configuration binding, .NET 6 and lower used to just call Add on the existing item.
    /// .NET 7 changed this to call the setter with an array that already starts with the old value.
    /// We have to handle both cases.
    /// </summary>
    /// <typeparam name="T">The List Type</typeparam>
    /// <param name="value">The set of values to be assigned</param>
    /// <returns>A IList of type T that will be consistent even if it has been set via Config</returns>
    public static IList<T> WithConfigBinding<T>(this IList<T> value)
        where T : StringOrRegex
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

/// <summary>
/// This class allows the TracePropagationTargets option to be set from config, such as appSettings.json
/// </summary>
internal class StringOrRegexTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        new StringOrRegex((string)value);
}
