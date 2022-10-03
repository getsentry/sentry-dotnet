using System;
using System.Text.RegularExpressions;

namespace Sentry
{
    /// <summary>
    /// Provides a pattern that can be used to identify which destinations will have <c>sentry-trace</c> and
    /// <c>baggage</c> headers propagated to, for purposes of distributed tracing.
    /// The pattern can either be a substring or a regular expression.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/performance/#tracepropagationtargets"/>
    public class TracePropagationTarget
    {
        private readonly Regex? _regex;
        private readonly string? _substring;
        private readonly StringComparison _stringComparison;

        private TracePropagationTarget(string substring, StringComparison comparison)
        {
            _substring = substring;
            _stringComparison = comparison;
        }

        private TracePropagationTarget(Regex regex) => _regex = regex;

        /// <summary>
        /// Creates a <see cref="TracePropagationTarget"/> instance that will match when the provided
        /// <paramref name="substring"/> is contained within the outgoing request URL.
        /// </summary>
        /// <param name="substring">The substring to match.</param>
        /// <param name="caseSensitive">
        /// Whether the matching is case sensitive. Defaults to <c>false</c> (case insensitive).
        /// </param>
        /// <returns>The constructed <see cref="TracePropagationTarget"/> instance.</returns>
        public static TracePropagationTarget CreateFromSubstring(string substring, bool caseSensitive = false) =>
            new(substring, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a <see cref="TracePropagationTarget"/> instance that will match when the provided
        /// <paramref name="pattern"/> is a regular expression pattern that matches the outgoing request URL.
        /// </summary>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="caseSensitive">
        /// Whether the matching is case sensitive. Defaults to <c>false</c> (case insensitive).
        /// </param>
        /// <returns>The constructed <see cref="TracePropagationTarget"/> instance.</returns>
        /// <remarks>
        /// Sets <see cref="RegexOptions.Compiled"/> and <see cref="RegexOptions.CultureInvariant"/> always.
        /// Sets <see cref="RegexOptions.IgnoreCase"/> when <paramref name="caseSensitive"/> is <c>true</c>.
        /// </remarks>
        public static TracePropagationTarget CreateFromRegex(string pattern, bool caseSensitive = false)
        {
            var regexOptions =
                RegexOptions.Compiled |
                RegexOptions.CultureInvariant |
                (caseSensitive ? RegexOptions.IgnoreCase : RegexOptions.None);

            var regex = new Regex(pattern, regexOptions);
            return new TracePropagationTarget(regex);
        }

        /// <summary>
        /// Creates a <see cref="TracePropagationTarget"/> instance that will match when the provided
        /// <paramref name="regex"/> is a regular expression object that matches the outgoing request URL.
        /// </summary>
        /// <param name="regex">The regular expression object to match.</param>
        /// <returns>The constructed <see cref="TracePropagationTarget"/> instance.</returns>
        /// <remarks>
        /// Use this overload when you need to control the regular expression matching options.
        /// We recommend setting at least <see cref="RegexOptions.Compiled"/> for performance, and
        /// <see cref="RegexOptions.CultureInvariant"/> (unless you have culture-specific matching needs).
        /// The <see cref="CreateFromRegex(string,bool)"/> overload sets these by default.
        /// </remarks>
        public static TracePropagationTarget CreateFromRegex(Regex regex) => new(regex);

        /// <inheritdoc />
        public override string ToString() => _substring ?? _regex?.ToString() ?? "";

        internal bool IsMatch(string url) =>
            _regex?.IsMatch(url) == true ||
            (_substring != null && url.Contains(_substring, _stringComparison));
    }
}
