using Sentry.Internal.Extensions;
using Sentry.Internal.OpenTelemetry;

namespace Sentry.Internal.Tracing;

// Ported from Sentry.OpenTelemetry.OpenTelemetryExtensions so that the core Activity processor has no
// dependency on the OpenTelemetry SDK package.
internal static class ActivityAttributeExtensions
{
    public static BaggageHeader AsBaggageHeader(this IEnumerable<KeyValuePair<string, string?>> baggage,
        bool useSentryPrefix = false) =>
        BaggageHeader.Create(
            baggage.Where(member => member.Value != null)
                .Select(kvp => (KeyValuePair<string, string>)kvp!),
            useSentryPrefix
        );

    /// <summary>
    /// The names that OpenTelemetry gives to attributes, by convention, have changed over time so we often need to
    /// check for both the new attribute and any obsolete ones.
    /// </summary>
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

    public static short? HttpResponseStatusCodeAttribute(this IDictionary<string, object?> attributes)
    {
        var statusCode = attributes.GetFirstMatchingAttribute<int?>(
            OtelSemanticConventions.AttributeHttpResponseStatusCode
        );
        return statusCode is >= short.MinValue and <= short.MaxValue
            ? (short)statusCode.Value
            : null;
    }
}
