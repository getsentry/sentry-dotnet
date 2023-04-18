internal static class JsonSerializableExtensions
{
    public static string ToJsonString(this IJsonSerializable serializable, IDiagnosticLogger logger = null, bool indented = false) =>
        WriteToJsonString(writer => writer.WriteSerializableValue(serializable, logger), indented);

    public static string ToJsonString(this object @object, IDiagnosticLogger logger = null, bool indented = false) =>
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

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
#else
        // This implementation is compatible with older targets
        using var stream = new MemoryStream();

        using var writer = new Utf8JsonWriter(stream, options);
        writeAction(writer);
        writer.Flush();

        // Using a reader will avoid copying to an intermediate byte array
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
#endif
    }
}
