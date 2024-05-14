using Sentry.Internal.Extensions;
using Sentry.Internal.OpenTelemetry;

namespace Sentry.OpenTelemetry;

internal static class OpenTelemetryExtensions
{
    public static SpanId AsSentrySpanId(this ActivitySpanId id) => SpanId.Parse(id.ToHexString());

    public static ActivitySpanId AsActivitySpanId(this SpanId id) => ActivitySpanId.CreateFromString(id.ToString().AsSpan());

    public static SentryId AsSentryId(this ActivityTraceId id) => SentryId.Parse(id.ToHexString());

    public static ActivityTraceId AsActivityTraceId(this SentryId id) => ActivityTraceId.CreateFromString(id.ToString().AsSpan());

    public static BaggageHeader AsBaggageHeader(this IEnumerable<KeyValuePair<string, string?>> baggage, bool useSentryPrefix = false) =>
        BaggageHeader.Create(
            baggage.Where(member => member.Value != null)
                        .Select(kvp => (KeyValuePair<string, string>)kvp!),
            useSentryPrefix
            );

    /// <summary>
    /// The names that OpenTelemetry gives to attributes, by convention, have changed over time so we often need to
    /// check for both the new attribute and any obsolete ones.
    /// </summary>
    /// <param name="attributes">The attributes to be searched</param>
    /// <param name="attributeNames">The list of possible names for the attribute you want to retrieve</param>
    /// <typeparam name="T">The expected type of the attribute</typeparam>
    /// <returns>The first attribute it finds matching one of the supplied <paramref name="attributeNames"/>
    /// or null, if no matching attribute is found
    /// </returns>
    private static T? GetFirstMatchingAttribute<T>(this IDictionary<string, object?> attributes,
        params string[] attributeNames)
    {
        foreach (var name in attributeNames)
        {
            if (attributes.TryGetTypedValue(name, out T value))
            {
                return value;
            }
        }
        return default;
    }

    public static string? HttpMethodAttribute(this IDictionary<string, object?> attributes) =>
        attributes.GetFirstMatchingAttribute<string>(
            OtelSemanticConventions.AttributeHttpRequestMethod,
            OtelSemanticConventions.AttributeHttpMethod // Fallback pre-1.5.0
            );

    public static string? UrlFullAttribute(this IDictionary<string, object?> attributes) =>
        attributes.GetFirstMatchingAttribute<string>(
            OtelSemanticConventions.AttributeUrlFull,
            OtelSemanticConventions.AttributeHttpUrl // Fallback pre-1.5.0
            );
}
