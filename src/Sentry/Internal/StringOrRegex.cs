namespace Sentry.Internal;

/// <summary>
/// Stores either a plain string or a Regular Expression, typically to match against filters in the SentryOptions
/// </summary>
internal class StringOrRegex
{
    internal readonly Regex? _regex;
    internal readonly string? _prefix;

    /// <summary>
    /// Constructs a <see cref="StringOrRegex"/> instance.
    /// </summary>
    /// <param name="stringOrRegex">The prefix or regular expression pattern to match on.</param>
    public StringOrRegex(string stringOrRegex)
    {
        _prefix = stringOrRegex;
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
    public override string ToString() => _prefix ?? _regex?.ToString() ?? "";

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
