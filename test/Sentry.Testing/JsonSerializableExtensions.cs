#nullable enable

namespace Sentry.Testing;

internal static class JsonSerializableExtensions
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static string ToJsonString(this ISentryJsonSerializable serializable, IDiagnosticLogger? logger = null, bool indented = false) =>
        WriteToJsonString(writer => writer.WriteSerializableValue(serializable, logger), indented);

    public static string ToJsonString(this object @object, IDiagnosticLogger? logger = null, bool indented = false) =>
        WriteToJsonString(writer => writer.WriteDynamicValue(@object, logger), indented);

    private static string WriteToJsonString(Action<Utf8JsonWriter> writeAction, bool indented)
    {
        var options = new JsonWriterOptions
        {
            Indented = indented
        };

#if NETCOREAPP3_0_OR_GREATER
        // This implementation is better, as it uses fewer allocations
        var buffer = new ArrayBufferWriter<byte>();

        using var writer = new Utf8JsonWriter(buffer, options);
        writeAction(writer);
        writer.Flush();

        var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
#else
        // This implementation is compatible with older targets
        using var stream = new MemoryStream();

        using var writer = new Utf8JsonWriter(stream, options);
        writeAction(writer);
        writer.Flush();

        // Using a reader will avoid copying to an intermediate byte array
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var result =  reader.ReadToEnd();
#endif

        // Standardize on \n on all platforms, for consistency in tests.
        return IsWindows ? result.Replace("\r\n", "\n") : result;
    }

    public static JsonDocument ToJsonDocument(this ISentryJsonSerializable serializable, IDiagnosticLogger? logger = null) =>
        WriteToJsonDocument(writer => writer.WriteSerializableValue(serializable, logger));

    public static JsonDocument ToJsonDocument<T>(this T @object, Action<T, Utf8JsonWriter, IDiagnosticLogger?> serialize, IDiagnosticLogger? logger = null) where T : class =>
        WriteToJsonDocument(writer => serialize.Invoke(@object, writer, logger));

    private static JsonDocument WriteToJsonDocument(Action<Utf8JsonWriter> writeAction)
    {
#if (NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        // This implementation is better, as it uses fewer allocations
        var buffer = new ArrayBufferWriter<byte>();

        using var writer = new Utf8JsonWriter(buffer);
        writeAction(writer);
        writer.Flush();

        return JsonDocument.Parse(buffer.WrittenMemory);
#else
        // This implementation is compatible with older targets
        using var stream = new MemoryStream();

        using var writer = new Utf8JsonWriter(stream);
        writeAction(writer);
        writer.Flush();

        stream.Seek(0, SeekOrigin.Begin);
        return JsonDocument.Parse(stream);
#endif
    }
}
