using Sentry.Extensibility;
using SentryCocoa;

namespace Sentry.iOS.Extensions;

internal static class CocoaExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this NSDate timestamp) => new((DateTime)timestamp);

    public static NSDate ToNSDate(this DateTimeOffset timestamp) => (NSDate)timestamp.UtcDateTime;

    public static string? ToJsonString(this NSObject? obj, IDiagnosticLogger? logger = null)
    {
        if (obj == null)
        {
            return null;
        }

        if (obj is ISentrySerializable serializable)
        {
            // For types that implement Sentry Cocoa's SentrySerializable protocol (interface),
            // We should call that first, and then serialize the result to JSON later.
            obj = serializable.Serialize();
        }

        // Now we will use Apple's JSON Serialization functions.
        // See https://developer.apple.com/documentation/foundation/nsjsonserialization

        if (!NSJsonSerialization.IsValidJSONObject(obj))
        {
            logger?.LogWarning("Cannot serialize a {0} directly to JSON", obj.GetType().Name);
            return null;
        }

        try
        {
            using var data = NSJsonSerialization.Serialize(obj, 0, out _);
            return data.ToString();
        }
        catch (Exception ex)
        {
            logger?.LogError("Error serializing {0} to JSON", ex, obj.GetType().Name);
            return null;
        }
    }

    public static Dictionary<string, string> ToStringDictionary<TValue>(
        this NSDictionary<NSString, TValue>? dict,
        IDiagnosticLogger? logger = null)
        where TValue : NSObject
    {
        if (dict == null)
        {
            return new Dictionary<string, string>();
        }

        var result = new Dictionary<string, string>(capacity: (int)dict.Count);
        foreach (var key in dict.Keys)
        {
            var value = dict[key];
            if (value is NSString s)
            {
                result.Add(key, s);
            }
            else if (value.ToJsonString(logger) is { } json)
            {
                result.Add(key, json);
            }

            // Skip null values, including anything that couldn't be serialized
        }

        return result;
    }

    public static Dictionary<string, string>? ToNullableStringDictionary<TValue>(
        this NSDictionary<NSString, TValue>? dict,
        IDiagnosticLogger? logger = null)
        where TValue : NSObject
    {
        if (dict is null || dict.Length == 0)
        {
            return null;
        }

        return dict.ToStringDictionary(logger);
    }

    public static NSDictionary<NSString, NSObject> ToNSDictionary<TValue>(
        this IEnumerable<KeyValuePair<string, TValue>> dict)
    {
        var d = new Dictionary<NSString, NSObject>();
        foreach (var item in dict)
        {
            // skip null values, but add others as NSObject
            if (item.Value is { } value)
            {
                d.Add((NSString)item.Key, NSObject.FromObject(value));
            }
        }

        return NSDictionary<NSString, NSObject>
            .FromObjectsAndKeys(
                d.Values.ToArray(),
                d.Keys.ToArray());
    }

    public static NSDictionary<NSString, NSObject>? ToNullableNSDictionary<TValue>(
        this IEnumerable<KeyValuePair<string, TValue>> dict)
    {
        var d = dict.ToNSDictionary();
        return d.Count == 0 ? null : d;
    }
}
