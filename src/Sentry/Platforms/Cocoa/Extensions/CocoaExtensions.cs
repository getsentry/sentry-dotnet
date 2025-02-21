using Sentry.Extensibility;

namespace Sentry.Cocoa.Extensions;

internal static class CocoaExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this NSDate timestamp) => new((DateTime)timestamp);

    public static NSDate ToNSDate(this DateTimeOffset timestamp) => (NSDate)timestamp.UtcDateTime;

    public static NSString ToNSString(this string str) => new NSString(str);

    public static string? ToJsonString(this NSObject? obj, IDiagnosticLogger? logger = null)
    {
        using var data = obj.ToJsonData(logger);
        return data?.ToString();
    }

    public static Stream? ToJsonStream(this NSObject? obj, IDiagnosticLogger? logger = null) =>
        obj.ToJsonData(logger)?.AsStream();

    private static NSData? ToJsonData(this NSObject? obj, IDiagnosticLogger? logger = null)
    {
        if (obj == null)
        {
            return null;
        }

        if (obj is CocoaSdk.ISentrySerializable serializable)
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
            return NSJsonSerialization.Serialize(obj, 0, out _);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error serializing {0} to JSON", obj.GetType().Name);
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
        if (dict is null || dict.Count == 0)
        {
            return null;
        }

        return dict.ToStringDictionary(logger);
    }

    public static Dictionary<string, object?> ToObjectDictionary<TValue>(
        this NSDictionary<NSString, TValue>? dict,
        IDiagnosticLogger? logger = null)
        where TValue : NSObject
    {
        if (dict == null)
        {
            return new Dictionary<string, object?>();
        }

        var result = new Dictionary<string, object?>(capacity: (int)dict.Count);
        foreach (var key in dict.Keys)
        {
            var value = dict[key];
            switch (value)
            {
                case null or NSNull:
                    result.Add(key, null);
                    break;
                case NSString s:
                    result.Add(key, s);
                    break;
                case NSNumber n:
                    result.Add(key, n.ToObject());
                    break;
                default:
                    if (value.ToJsonString(logger) is { } json)
                    {
                        result.Add(key, json);
                    }
                    else
                    {
                        logger?.LogWarning("Could not add value of type {0} to dictionary.", value.GetType());
                    }

                    break;
            }
        }

        return result;
    }

    public static Dictionary<string, object?>? ToNullableObjectDictionary<TValue>(
        this NSDictionary<NSString, TValue>? dict,
        IDiagnosticLogger? logger = null)
        where TValue : NSObject
    {
        if (dict is null || dict.Count == 0)
        {
            return null;
        }

        return dict.ToObjectDictionary(logger);
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

    public static NSDictionary<NSString, NSString> ToNSDictionaryStrings(
        this IEnumerable<KeyValuePair<string, string>> dict)
    {
        var d = new Dictionary<NSString, NSString>();
        foreach (var item in dict)
        {
            if (item.Value != null)
            {
                d.Add((NSString)item.Key, new NSString(item.Value));
            }
        }

        return NSDictionary<NSString, NSString>
            .FromObjectsAndKeys(
                d.Values.ToArray(),
                d.Keys.ToArray());
    }

    public static NSDictionary<NSString, NSObject>? ToNullableNSDictionary<TValue>(
        this ICollection<KeyValuePair<string, TValue>> dict) =>
        dict.Count == 0 ? null : dict.ToNSDictionary();

    public static NSDictionary<NSString, NSObject>? ToNullableNSDictionary<TValue>(
        this IReadOnlyCollection<KeyValuePair<string, TValue>> dict) =>
        dict.Count == 0 ? null : dict.ToNSDictionary();

    /// <summary>
    /// Converts an <see cref="NSNumber"/> to a .NET primitive data type and returns the result box in an <see cref="object"/>.
    /// </summary>
    /// <param name="n">The <see cref="NSNumber"/> to convert.</param>
    /// <returns>An <see cref="object"/> that contains the number in its primitive type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the number's <c>ObjCType</c> was unrecognized.</exception>
    /// <remarks>
    /// This method always returns a result that is compatible with its value, but does not always give the expected result.
    /// Specifically:
    /// <list type="bullet">
    ///   <item><c>byte</c> returns <c>short</c></item>
    ///   <item><c>ushort</c> return <c>int</c></item>
    ///   <item><c>uint</c> returns <c>long</c></item>
    ///   <item><c>ulong</c> returns <c>long</c> unless it's > <c>long.MaxValue</c></item>
    ///   <item>n/nu types return more primitive types (ex. <c>nfloat</c> => <c>double</c>)</item>
    /// </list>
    /// Type encodings are listed here:
    /// https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/ObjCRuntimeGuide/Articles/ocrtTypeEncodings.html
    /// </remarks>
    public static object ToObject(this NSNumber n)
    {
        if (n is NSDecimalNumber d)
        {
            // handle NSDecimalNumber type directly
            return (decimal)d.NSDecimalValue;
        }

        return n.ObjCType switch
        {
            "c" when n.Class.Name == "__NSCFBoolean" => n.BoolValue, // ObjC bool
            "c" => n.SByteValue, // signed char
            "i" => n.Int32Value, // signed int
            "s" => n.Int16Value, // signed short
            "l" => n.Int32Value, // signed long (32 bit)
            "q" => n.Int64Value, // signed long long (64 bit)
            "C" => n.ByteValue, // unsigned char
            "I" => n.UInt32Value, // unsigned int
            "S" => n.UInt16Value, // unsigned short
            "L" => n.UInt32Value, // unsigned long (32 bit)
            "Q" => n.UInt64Value, // unsigned long long (64 bit)
            "f" => n.FloatValue, // float
            "d" => n.DoubleValue, // double
            "B" => n.BoolValue, // C++ bool or C99 _Bool
            _ => throw new ArgumentOutOfRangeException(nameof(n), n,
                $"NSNumber \"{n.StringValue}\" has an unknown ObjCType \"{n.ObjCType}\" (Class: \"{n.Class.Name}\")")
        };
    }


    public static SentryEvent? ToSentryEvent(this CocoaSdk.SentryEvent sentryEvent)
    {
        using var stream = sentryEvent.ToJsonStream();
        if (stream == null)
            return null;

        using var json = JsonDocument.Parse(stream);
        var exception = sentryEvent.Error == null ? null : new NSErrorException(sentryEvent.Error);
        var ev = SentryEvent.FromJson(json.RootElement, exception);
        return ev;
    }

    public static CocoaSdk.SentryMessage ToCocoaSentryMessage(this SentryMessage msg)
    {
        var native = new CocoaSdk.SentryMessage(msg.Formatted ?? string.Empty);
        native.Params = msg.Params?.Select(x => x.ToString()!).ToArray() ?? new string[0];

        return native;
    }

    // not tested or needed yet - leaving for future just in case
    // public static CocoaSdk.SentryThread ToCocoaSentryThread(this SentryThread thread)
    // {
    //     var id = NSNumber.FromInt32(thread.Id ?? 0);
    //     var native = new CocoaSdk.SentryThread(id);
    //     native.Crashed = thread.Crashed;
    //     native.Current = thread.Current;
    //     native.Name = thread.Name;
    //     native.Stacktrace = thread.Stacktrace?.ToCocoaSentryStackTrace();
    //     // native.IsMain = not in dotnet
    //     return native;
    // }
    //
    // public static CocoaSdk.SentryRequest ToCocoaSentryRequest(this SentryRequest request)
    // {
    //     var native = new CocoaSdk.SentryRequest();
    //     native.Cookies = request.Cookies;
    //     native.Headers = request.Headers?.ToNSDictionaryStrings();
    //     native.Method = request.Method;
    //     native.QueryString = request.QueryString;
    //     native.Url = request.Url;
    //
    //     // native.BodySize does not exist in dotnet
    //     return native;
    // }
    //

    // public static CocoaSdk.SentryException ToCocoaSentryException(this SentryException ex)
    // {
    //     var native = new CocoaSdk.SentryException(ex.Value ?? string.Empty, ex.Type ?? string.Empty);
    //     native.Module = ex.Module;
    //     native.Mechanism = ex.Mechanism?.ToCocoaSentryMechanism();
    //     native.Stacktrace = ex.Stacktrace?.ToCocoaSentryStackTrace();
    //     // not part of native - ex.ThreadId;
    //     return native;
    // }
    //
    // public static CocoaSdk.SentryStacktrace ToCocoaSentryStackTrace(this SentryStackTrace stackTrace)
    // {
    //     var frames = stackTrace.Frames?.Select(x => x.ToCocoaSentryFrame()).ToArray() ?? new CocoaSdk.SentryFrame[0];
    //     var native = new CocoaSdk.SentryStacktrace(frames, new NSDictionary<NSString, NSString>());
    //     // native.Register & native.Snapshot missing in dotnet
    //     return native;
    // }
    //
    // public static CocoaSdk.SentryFrame ToCocoaSentryFrame(this SentryStackFrame frame)
    // {
    //     var native = new CocoaSdk.SentryFrame();
    //     native.Module = frame.Module;
    //     native.Package = frame.Package;
    //     native.InstructionAddress = frame.InstructionAddress?.ToString();
    //     native.Function = frame.Function;
    //     native.Platform = frame.Platform;
    //     native.ColumnNumber = frame.ColumnNumber;
    //     native.FileName = frame.FileName;
    //     native.InApp = frame.InApp;
    //     native.ImageAddress = frame.ImageAddress?.ToString();
    //     native.LineNumber = frame.LineNumber;
    //     native.SymbolAddress = frame.SymbolAddress?.ToString();
    //
    //     // native.StackStart = doesn't exist in dotnet
    //     return native;
    // }
    //
    // public static CocoaSdk.SentryMechanism ToCocoaSentryMechanism(this Mechanism mechanism)
    // {
    //     var native = new CocoaSdk.SentryMechanism(mechanism.Type);
    //     native.Synthetic = mechanism.Synthetic;
    //     native.Handled = mechanism.Handled;
    //     native.Desc = mechanism.Description;
    //     native.HelpLink = mechanism.HelpLink;
    //     native.Data = mechanism.Data?.ToNSDictionary();
    //     // TODO: Meta does not currently translate in dotnet - native.Meta = null;
    //     return native;
    // }
}
