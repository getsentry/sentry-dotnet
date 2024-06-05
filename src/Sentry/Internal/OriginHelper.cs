namespace Sentry.Internal;

internal static partial class OriginHelper
{
    internal const string Manual = "manual";
    private const string ValidOriginPattern = @"^(auto|manual)(\.[\w_-]+){0,3}$";

#if NET8_0_OR_GREATER
    [GeneratedRegex(ValidOriginPattern, RegexOptions.Compiled)]
    private static partial Regex ValidOriginRegEx();
    private static readonly Regex ValidOrigin = ValidOriginRegEx();
#else
    private static readonly Regex ValidOrigin = new (ValidOriginPattern, RegexOptions.Compiled);
#endif

    public static bool IsValidOrigin(string? value) => value == null || ValidOrigin.IsMatch(value);

    internal static string? TryParse(string origin) => IsValidOrigin(origin) ? origin : null;

    /// <summary>
    /// Convenience method to let us set the origin on interfaces whose concrete implementations
    /// typically have an Origin property.
    /// </summary>
    /// <remarks>We can remove once we deprecate the ITraceContextInternal interface</remarks>
    public static void SetOrigin(this ISpan span, string origin)
    {
        switch (span)
        {
            case SpanTracer spanTracer:
                spanTracer.Origin = origin;
                break;
            case TransactionTracer transactionTracer:
                transactionTracer.Contexts.Trace.Origin = origin;
                break;
        }
    }
}
