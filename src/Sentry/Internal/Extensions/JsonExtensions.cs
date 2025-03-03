using Sentry.Extensibility;
using Sentry.Internal.JsonConverters;

namespace Sentry.Internal.Extensions;

internal static class JsonExtensions
{
    private static readonly JsonConverter[] DefaultConverters =
    {
        new SentryJsonConverter(),
        new IntPtrJsonConverter(),
        new IntPtrNullableJsonConverter(),
        new UIntPtrJsonConverter(),
        new UIntPtrNullableJsonConverter()
    };

    private static List<JsonConverter> CustomConverters = new List<JsonConverter>();

    internal static bool JsonPreserveReferences { get; set; } = true;

    static JsonExtensions()
    {
        ResetSerializerOptions();
    }

    private static JsonSerializerOptions BuildOptions(bool preserveReferences)
    {
        var options = new JsonSerializerOptions();
        if (preserveReferences)
        {
            options.ReferenceHandler = ReferenceHandler.Preserve;
        }
        foreach (var converter in DefaultConverters)
        {
            options.Converters.Add(converter);
        }
        foreach (var converter in CustomConverters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }

    private static JsonSerializerOptions SerializerOptions = null!;
    private static JsonSerializerOptions AltSerializerOptions = null!;

    private static List<JsonSerializerContext> DefaultSerializerContexts = new();
    private static List<JsonSerializerContext> ReferencePreservingSerializerContexts = new();

    private static List<Func<JsonSerializerOptions, JsonSerializerContext>> JsonSerializerContextBuilders = new()
    {
        options => new SentryJsonContext(options)
    };

    internal static void AddJsonSerializerContext<T>(Func<JsonSerializerOptions, T> jsonSerializerContextBuilder)
        where T : JsonSerializerContext
    {
        JsonSerializerContextBuilders.Add(jsonSerializerContextBuilder);
        ResetSerializerOptions();
    }

    internal static void ResetSerializerOptions()
    {
        // For our classic reflection based serialization
        SerializerOptions = BuildOptions(false);
        AltSerializerOptions = BuildOptions(true);

        // For the new AOT serialization
        DefaultSerializerContexts.Clear();
        ReferencePreservingSerializerContexts.Clear();
        foreach (var builder in JsonSerializerContextBuilders)
        {
            DefaultSerializerContexts.Add(builder(BuildOptions(false)));
            ReferencePreservingSerializerContexts.Add(builder(BuildOptions(true)));
        }
    }

    internal static void AddJsonConverter(JsonConverter converter)
    {
        // only add if we don't have this instance already
        if (CustomConverters.Contains(converter))
        {
            return;
        }

        try
        {
            CustomConverters.Add(converter);
            ResetSerializerOptions();
        }
        catch (InvalidOperationException)
        {
            // If we've already started using the serializer, then it's too late to add more converters.
            // The following exception message may occur (depending on STJ version):
            // "Serializer options cannot be changed once serialization or deserialization has occurred."
            // We'll swallow this, because it's likely to only have occurred in our own unit tests,
            // or in a scenario where the Sentry SDK has been initialized multiple times,
            // in which case we have the converter from the first initialization already.
            // TODO: .NET 8 is getting an IsReadOnly flag we could check instead of catching
            // See https://github.com/dotnet/runtime/pull/74431
        }
    }

    public static void Deconstruct(this JsonProperty jsonProperty, out string name, out JsonElement value)
    {
        name = jsonProperty.Name;
        value = jsonProperty.Value;
    }

    public static Dictionary<string, object?>? GetDictionaryOrNull(this JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var result = new Dictionary<string, object?>();

        foreach (var (name, value) in json.EnumerateObject())
        {
            result[name] = value.GetDynamicOrNull();
        }

        return result;
    }

    public static Dictionary<string, TValue>? GetDictionaryOrNull<TValue>(
        this JsonElement json,
        Func<JsonElement, TValue> factory)
        where TValue : ISentryJsonSerializable?
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var result = new Dictionary<string, TValue>();

        foreach (var (name, value) in json.EnumerateObject())
        {
            result[name] = factory(value);
        }

        return result;
    }

    public static Dictionary<string, string?>? GetStringDictionaryOrNull(this JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var result = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var (name, value) in json.EnumerateObject())
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                result[name] = value.GetString();
            }
            else
            {
                result[name] = value.ToString();
            }
        }

        return result;
    }

    public static JsonElement? GetPropertyOrNull(this JsonElement json, string name)
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (json.TryGetProperty(name, out var result) &&
            result.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            return result;
        }

        return null;
    }

    public static object? GetDynamicOrNull(this JsonElement json) => json.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => json.GetNumber(), // see implementation for why we don't just call GetDouble
        JsonValueKind.String => json.GetString(),
        JsonValueKind.Array => json.EnumerateArray().Select(GetDynamicOrNull).ToArray(),
        JsonValueKind.Object => json.GetDictionaryOrNull(),
        _ => null
    };

    private static object? GetNumber(this JsonElement json)
    {
        var result = json.GetDouble();
        if (result != 0)
        {
            // We got a value, as expected.
            return result;
        }

        // We might have 0 when there's actually a value there.
        // This happens on Unity IL2CPP targets.  Let's workaround that.
        // See https://github.com/getsentry/sentry-unity/issues/690

        // If the number is an integer, we can avoid extra string parsing
        if (json.TryGetInt64(out var longResult))
        {
            return longResult;
        }

        // Otherwise, let's get the value as a string and parse it ourselves.
        // Note that we already know this will succeed due to JsonValueKind.Number
        return double.Parse(json.ToString()!, CultureInfo.InvariantCulture);
    }

    public static long? GetHexAsLong(this JsonElement json)
    {
        // If the address is in json as a number, we can just use it.
        if (json.ValueKind == JsonValueKind.Number)
        {
            return json.GetInt64();
        }

        // Otherwise it will be a string, but we need to convert it to a number.
        var s = json.GetString();
        if (s == null)
        {
            return null;
        }

        // It should be in hex format, such as "0x7fff5bf346c0"
        var substring = s[2..];
        if (s.StartsWith("0x") &&
            long.TryParse(substring, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        throw new FormatException();
    }

    public static string GetStringOrThrow(this JsonElement json) =>
        json.GetString() ?? throw new InvalidOperationException("JSON string is null.");

    public static void WriteDictionaryValue(
        this Utf8JsonWriter writer,
        IEnumerable<KeyValuePair<string, object?>>? dic,
        IDiagnosticLogger? logger,
        bool includeNullValues = true)
    {
        if (dic is not null)
        {
            writer.WriteStartObject();

            if (includeNullValues)
            {
                foreach (var (key, value) in dic)
                {
                    writer.WriteDynamic(key, value, logger);
                }
            }
            else
            {
                foreach (var (key, value) in dic)
                {
                    if (value is not null)
                    {
                        writer.WriteDynamic(key, value, logger);
                    }
                }
            }

            writer.WriteEndObject();
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public static void WriteDictionaryValue<TValue>(
        this Utf8JsonWriter writer,
        IEnumerable<KeyValuePair<string, TValue>>? dic,
        IDiagnosticLogger? logger,
        bool includeNullValues = true)
        where TValue : ISentryJsonSerializable?
    {
        if (dic is not null)
        {
            writer.WriteStartObject();

            foreach (var (key, value) in dic)
            {
                if (value is not null)
                {
                    writer.WriteSerializable(key, value, logger);
                }
                else if (includeNullValues)
                {
                    writer.WriteNull(key);
                }
            }

            writer.WriteEndObject();
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public static void WriteStringDictionaryValue(
        this Utf8JsonWriter writer,
        IEnumerable<KeyValuePair<string, string?>>? dic)
    {
        if (dic is not null)
        {
            writer.WriteStartObject();

            foreach (var (key, value) in dic)
            {
                writer.WriteString(key, value);
            }

            writer.WriteEndObject();
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public static void WriteDictionary(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, object?>>? dic,
        IDiagnosticLogger? logger)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteDictionaryValue(dic, logger);
    }

    public static void WriteDictionary<TValue>(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, TValue>>? dic,
        IDiagnosticLogger? logger)
        where TValue : ISentryJsonSerializable?
    {
        writer.WritePropertyName(propertyName);
        writer.WriteDictionaryValue(dic, logger);
    }

    public static void WriteStringDictionary(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, string?>>? dic)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStringDictionaryValue(dic);
    }

    public static void WriteArrayValue<T>(
        this Utf8JsonWriter writer,
        IEnumerable<T>? arr,
        IDiagnosticLogger? logger)
    {
        if (arr is not null)
        {
            writer.WriteStartArray();

            foreach (var i in arr)
            {
                writer.WriteDynamicValue(i, logger);
            }

            writer.WriteEndArray();
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public static void WriteArray<T>(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<T>? arr,
        IDiagnosticLogger? logger)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteArrayValue(arr, logger);
    }

    public static void WriteStringArrayValue(
        this Utf8JsonWriter writer,
        IEnumerable<string?>? arr)
    {
        if (arr is not null)
        {
            writer.WriteStartArray();

            foreach (var i in arr)
            {
                writer.WriteStringValue(i);
            }

            writer.WriteEndArray();
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public static void WriteStringArray(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<string?>? arr)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStringArrayValue(arr);
    }

    public static void WriteSerializableValue(
        this Utf8JsonWriter writer,
        ISentryJsonSerializable value,
        IDiagnosticLogger? logger)
    {
        value.WriteTo(writer, logger);
    }

    public static void WriteSerializable(
        this Utf8JsonWriter writer,
        string propertyName,
        ISentryJsonSerializable value,
        IDiagnosticLogger? logger)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteSerializableValue(value, logger);
    }

    public static void WriteDynamicValue(
        this Utf8JsonWriter writer,
        object? value,
        IDiagnosticLogger? logger)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else if (value is ISentryJsonSerializable serializable)
        {
            writer.WriteSerializableValue(serializable, logger);
        }
        else if (value is IEnumerable<KeyValuePair<string, string?>> sdic)
        {
            writer.WriteStringDictionaryValue(sdic);
        }
        else if (value is IEnumerable<KeyValuePair<string, object?>> dic)
        {
            writer.WriteDictionaryValue(dic, logger);
        }
        else if (value is string str)
        {
            writer.WriteStringValue(str);
        }
        else if (value is bool b)
        {
            writer.WriteBooleanValue(b);
        }
        else if (value is int i)
        {
            writer.WriteNumberValue(i);
        }
        else if (value is long l)
        {
            writer.WriteNumberValue(l);
        }
        else if (value is double d)
        {
            writer.WriteNumberValue(d);
        }
        else if (value is DateTime dt)
        {
            writer.WriteStringValue(dt);
        }
        else if (value is DateTimeOffset dto)
        {
            writer.WriteStringValue(dto);
        }
        else if (value is TimeSpan timeSpan)
        {
            writer.WriteStringValue(timeSpan.ToString("g", CultureInfo.InvariantCulture));
        }
#if NET6_0_OR_GREATER
        else if (value is DateOnly date)
        {
            writer.WriteStringValue(date.ToString("O", CultureInfo.InvariantCulture));
        }
        else if (value is TimeOnly time)
        {
            writer.WriteStringValue(time.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture));
        }
#endif
        else if (value is IFormattable formattable)
        {
            writer.WriteStringValue(formattable.ToString(null, CultureInfo.InvariantCulture));
        }
        else if (value.GetType().ToString() == "System.RuntimeType")
        {
            writer.WriteStringValue(value.ToString());
        }
        else
        {
            if (!JsonPreserveReferences)
            {
                InternalSerialize(writer, value, preserveReferences: false);
                return;
            }

            try
            {
                // Use an intermediate byte array, so we can retry if serialization fails.
                var bytes = InternalSerializeToUtf8Bytes(value);
                writer.WriteRawValue(bytes);
            }
            catch (JsonException)
            {
                // Retry, preserving references to avoid cyclical dependency.
                InternalSerialize(writer, value, preserveReferences: true);
            }
        }
    }

    internal static string ToUtf8Json(this object value, bool preserveReferences = false)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        InternalSerialize(writer, value, preserveReferences);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static JsonSerializerContext GetSerializerContext(Type type, bool preserveReferences = false)
    {
        var contexts = preserveReferences ? ReferencePreservingSerializerContexts : DefaultSerializerContexts;
        return contexts.FirstOrDefault(c => c.GetTypeInfo(type) != null)
            ?? contexts[0]; // If none of the contexts has type info, this gives us a proper exception message
    }

    private static byte[] InternalSerializeToUtf8Bytes(object value)
    {
#if NET8_0_OR_GREATER
        byte[] AotSerializeToUtf8Bytes()
        {
            var context = GetSerializerContext(value.GetType());
            return JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), context);
        }
        return JsonSerializer.IsReflectionEnabledByDefault
            ? JitSerializeToUtf8Bytes()
            : AotSerializeToUtf8Bytes();
#else
        return JitSerializeToUtf8Bytes();
#endif

        [UnconditionalSuppressMessage("Trimming", "IL2026: RequiresUnreferencedCode", Justification = AotHelper.AvoidAtRuntime)]
        [UnconditionalSuppressMessage("AOT", "IL3050: RequiresDynamicCode", Justification = AotHelper.AvoidAtRuntime)]
        byte[] JitSerializeToUtf8Bytes() => JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
    }

    private static void InternalSerialize(Utf8JsonWriter writer, object value, bool preserveReferences = false)
    {
#if NET8_0_OR_GREATER
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            JitSerialize();
        }
        else
        {
            var context = GetSerializerContext(value.GetType(), preserveReferences);
            JsonSerializer.Serialize(writer, value, value.GetType(), context);
        }
#else
        JitSerialize();
#endif
        return;

        [UnconditionalSuppressMessage("AOT", "IL3050: RequiresDynamicCode", Justification = AotHelper.AvoidAtRuntime)]
        [UnconditionalSuppressMessage("Trimming", "IL2026: RequiresUnreferencedCode", Justification = AotHelper.AvoidAtRuntime)]
        void JitSerialize()
        {
            var options = preserveReferences ? AltSerializerOptions : SerializerOptions;
            JsonSerializer.Serialize(writer, value, options);
        }
    }

    public static void WriteDynamic(
        this Utf8JsonWriter writer,
        string propertyName,
        object? value,
        IDiagnosticLogger? logger)
    {
        writer.WritePropertyName(propertyName);
        var originalPropertyDepth = writer.CurrentDepth;
        try
        {
            writer.WriteDynamicValue(value, logger);
        }
        catch (Exception e)
        {
            // In the event of an instance that can't be serialized, we don't want to throw away a whole event
            // so we'll suppress issues here.
            logger?.LogError(e, "Failed to serialize object for property '{0}'. Original depth: {1}, current depth: {2}",
                propertyName, originalPropertyDepth, writer.CurrentDepth);

            // The only location in the protocol we allow dynamic objects are Extra and Contexts.
            // Render an empty JSON object instead of null. This allows a round trip where this property name is the
            // key to a map which would otherwise not be set and result in a different object.
            // This affects envelope size which isn't recomputed after a roundtrip.

            // If the last token written was ":", then we must write a property value.
            // If the last token written was "{", then we can't write a property value.
            // Since either could happen, we will *try* to write a "{" and ignore any failure.
            try
            {
                writer.WriteStartObject();
            }
            catch (InvalidOperationException)
            {
            }

            // Now we can close each open object until we get back to the original depth.
            while (originalPropertyDepth < writer.CurrentDepth)
            {
                writer.WriteEndObject();
            }
        }
    }

    public static void WriteBooleanIfNotNull(
        this Utf8JsonWriter writer,
        string propertyName,
        bool? value)
    {
        if (value is not null)
        {
            writer.WriteBoolean(propertyName, value.Value);
        }
    }

    public static void WriteBooleanIfTrue(
        this Utf8JsonWriter writer,
        string propertyName,
        bool? value)
    {
        if (value is true)
        {
            writer.WriteBoolean(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotNull(
        this Utf8JsonWriter writer,
        string propertyName,
        short? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotNull(
        this Utf8JsonWriter writer,
        string propertyName,
        int? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotNull(
        this Utf8JsonWriter writer,
        string propertyName,
        long? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotNull(
        this Utf8JsonWriter writer,
        string propertyName,
        float? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotNull(
        this Utf8JsonWriter writer,
        string propertyName,
        double? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    public static void WriteNumberIfNotZero(
        this Utf8JsonWriter writer,
        string propertyName,
        short value)
    {
        if (value is not 0)
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    public static void WriteNumberIfNotZero(
        this Utf8JsonWriter writer,
        string propertyName,
        int value)
    {
        if (value is not 0)
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    public static void WriteNumberIfNotZero(
        this Utf8JsonWriter writer,
        string propertyName,
        long value)
    {
        if (value is not 0)
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    public static void WriteNumberIfNotZero(
        this Utf8JsonWriter writer,
        string propertyName,
        float value)
    {
        if (value is not 0)
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    public static void WriteNumberIfNotZero(
        this Utf8JsonWriter writer,
        string propertyName,
        double value)
    {
        if (value is not 0)
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    public static void WriteStringIfNotWhiteSpace(
        this Utf8JsonWriter writer,
        string propertyName,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            writer.WriteString(propertyName, value);
        }
    }

    public static void WriteStringIfNotNull(
        this Utf8JsonWriter writer,
        string propertyName,
        DateTimeOffset? value)
    {
        if (value is not null)
        {
            writer.WriteString(propertyName, value.Value);
        }
    }

    public static void WriteSerializableIfNotNull(
        this Utf8JsonWriter writer,
        string propertyName,
        ISentryJsonSerializable? value,
        IDiagnosticLogger? logger)
    {
        if (value is not null)
        {
            writer.WriteSerializable(propertyName, value, logger);
        }
    }

    public static void WriteDictionaryIfNotEmpty(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, object?>>? dic,
        IDiagnosticLogger? logger)
    {
        var dictionary = dic as IReadOnlyDictionary<string, object?> ?? dic?.ToDict();
        if (dictionary is not null && dictionary.Count > 0)
        {
            writer.WriteDictionary(propertyName, dictionary, logger);
        }
    }

    public static void WriteDictionaryIfNotEmpty<TValue>(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, TValue>>? dic,
        IDiagnosticLogger? logger)
        where TValue : ISentryJsonSerializable?
    {
        var dictionary = dic as IReadOnlyDictionary<string, TValue> ?? dic?.ToDict();
        if (dictionary is not null && dictionary.Count > 0)
        {
            writer.WriteDictionary(propertyName, dictionary, logger);
        }
    }

    public static void WriteStringDictionaryIfNotEmpty(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<KeyValuePair<string, string?>>? dic)
    {
        var dictionary = dic as IReadOnlyDictionary<string, string?> ?? dic?.ToDict();
        if (dictionary is not null && dictionary.Count > 0)
        {
            writer.WriteStringDictionary(propertyName, dictionary);
        }
    }

    public static void WriteArrayIfNotEmpty<T>(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<T>? arr,
        IDiagnosticLogger? logger)
    {
        var list = arr as IReadOnlyList<T> ?? arr?.ToArray();
        if (list is not null && list.Count > 0)
        {
            writer.WriteArray(propertyName, list, logger);
        }
    }

    public static void WriteStringArrayIfNotEmpty(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<string?>? arr)
    {
        var list = arr as IReadOnlyList<string?> ?? arr?.ToArray();
        if (list is not null && list.Count > 0)
        {
            writer.WriteStringArray(propertyName, list);
        }
    }

    public static void WriteDynamicIfNotNull(
        this Utf8JsonWriter writer,
        string propertyName,
        object? value,
        IDiagnosticLogger? logger)
    {
        if (value is not null)
        {
            writer.WriteDynamic(propertyName, value, logger);
        }
    }

    public static void WriteString(
        this Utf8JsonWriter writer,
        string propertyName,
        IEnumeration? value)
    {
        if (value == null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, value.Value);
        }
    }
}

[JsonSerializable(typeof(GrowableArray<int>))]
[JsonSerializable(typeof(Dictionary<string, bool>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class SentryJsonContext : JsonSerializerContext
{
}
