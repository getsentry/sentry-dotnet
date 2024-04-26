using System;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Provides a pattern that can be used to match against namespaces as either a prefix or regular expression.
/// </summary>
internal class PrefixOrRegexPattern
{
    private readonly Regex? _regex;
    private readonly string? _prefix;
    private readonly StringComparison _stringComparison;

    /// <summary>
    /// Constructs a <see cref="PrefixOrRegexPattern"/> instance.
    /// </summary>
    /// <param name="prefixOrRegexPattern">The prefix or regular expression pattern to match on.</param>
    /// <param name="comparison">The string comparison type to use when matching.</param>
    public PrefixOrRegexPattern(
        string prefixOrRegexPattern,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        _prefix = prefixOrRegexPattern;
        _stringComparison = comparison;
    }

    /// <summary>
    /// Constructs a <see cref="PrefixOrRegexPattern"/> instance.
    /// </summary>
    /// <param name="regex"></param>
    /// <remarks>
    /// Use this constructor when you want the match to be performed using a regular expression.
    /// </remarks>
    public PrefixOrRegexPattern(Regex regex) => _regex = regex;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="PrefixOrRegexPattern"/>.
    /// </summary>
    /// <param name="prefixOrRegexPattern"></param>
    public static implicit operator PrefixOrRegexPattern(string prefixOrRegexPattern)
    {
        return new PrefixOrRegexPattern(prefixOrRegexPattern);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Regex"/> to a <see cref="PrefixOrRegexPattern"/>.
    /// </summary>
    /// <param name="regex"></param>
    public static implicit operator PrefixOrRegexPattern(Regex regex)
    {
        return new PrefixOrRegexPattern(regex);
    }

    /// <inheritdoc />
    public override string ToString() => _prefix ?? _regex?.ToString() ?? "";

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return
            (obj is PrefixOrRegexPattern pattern)
            && pattern.ToString() == ToString();
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    internal bool IsMatch(string str, char? requiredSeparator = null)
    {
        if (requiredSeparator is not {} separator)
        {
            return _prefix == ".*" || // perf shortcut
                   (_prefix != null && str.StartsWith(_prefix, _stringComparison)) ||
                   _regex?.IsMatch(str) == true;
        }

        if (_regex is null)
        {
            // Check for a prefix followed by the separator
            return _prefix != null && str.StartsWith(_prefix, _stringComparison) &&
                   str.Length > _prefix!.Length && str[_prefix.Length] == separator;
        }

        // Check for any regex match followed by the separator
        foreach (Match match in _regex.Matches(str))
        {
            if (str.Length > match.Value.Length && str[match.Value.Length] == separator)
            {
                return true;
            }
        }

        return false;
    }
}
