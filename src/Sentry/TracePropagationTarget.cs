namespace Sentry;

/// <summary>
/// Provides a pattern that can be used to identify which destinations will have <c>sentry-trace</c> and
/// <c>baggage</c> headers propagated to, for purposes of distributed tracing.
/// The pattern can either be a substring or a regular expression.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/performance/#tracepropagationtargets"/>
[TypeConverter(typeof(TracePropagationTargetTypeConverter))]
public class TracePropagationTarget: SubstringOrRegexPattern
{
    /// <summary>
    /// Constructs a <see cref="TracePropagationTarget"/> instance that will match when the provided
    /// <paramref name="substringOrRegexPattern"/> is either found as a substring within the outgoing request URL,
    /// or matches as a regular expression pattern against the outgoing request URL.
    /// </summary>
    /// <param name="substringOrRegexPattern">The substring or regular expression pattern to match on.</param>
    /// <param name="comparison">The string comparison type to use when matching.</param>
    public TracePropagationTarget(
        string substringOrRegexPattern,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        : base(substringOrRegexPattern, comparison)
    {
    }

    /// <summary>
    /// Constructs a <see cref="TracePropagationTarget"/> instance that will match when the provided
    /// <paramref name="regex"/> object matches the outgoing request URL.
    /// </summary>
    /// <param name="regex"></param>
    /// <remarks>
    /// Use this constructor when you need to control the regular expression matching options.
    /// We recommend setting at least <see cref="RegexOptions.Compiled"/> for performance, and
    /// <see cref="RegexOptions.CultureInvariant"/> (unless you have culture-specific matching needs).
    /// The <see cref="TracePropagationTarget(string, StringComparison)"/> constructor sets these by default.
    /// </remarks>
    public TracePropagationTarget(Regex regex) : base (regex)
    {
    }
}

//internal static class TracePropagationTargetExtensions
//{
//    public static bool ShouldPropagateTrace(this IEnumerable<TracePropagationTarget> targets, string url) =>
//        targets.Any(t => t.IsMatch(url));
//}

/// <summary>
/// Allows the TracePropagationTargets option to be set from config, such as appSettings.json
/// </summary>
internal class TracePropagationTargetTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        new TracePropagationTarget((string)value);
}
