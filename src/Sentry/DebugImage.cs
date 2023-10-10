using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// The Sentry Debug Meta Images interface.
/// </summary>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/debugmeta#debug-images"/>
public sealed class DebugImage : IJsonSerializable
{
    /// <summary>
    /// Type of the debug image.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Memory address, at which the image is mounted in the virtual address space of the process.
    /// </summary>
    public long? ImageAddress { get; set; }

    /// <summary>
    /// The size of the image in virtual memory.
    /// If missing, Sentry will assume that the image spans up to the next image, which might lead to invalid stack traces.
    /// </summary>
    public long? ImageSize { get; set; }

    /// <summary>
    /// Unique debug identifier of the image.
    /// </summary>
    public string? DebugId { get; set; }

    /// <summary>
    /// Checksum of the companion debug file.
    /// </summary>
    public string? DebugChecksum { get; set; }

    /// <summary>
    /// Path and name of the debug companion file.
    /// </summary>
    public string? DebugFile { get; set; }

    /// <summary>
    /// Optional identifier of the code file.
    /// </summary>
    public string? CodeId { get; set; }

    /// <summary>
    /// The absolute path to the dynamic library or executable.
    /// This helps to locate the file if it is missing on Sentry.
    /// </summary>
    public string? CodeFile { get; set; }

    internal Guid? ModuleVersionId { get; set; }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteStringIfNotWhiteSpace("type", Type);
        writer.WriteStringIfNotWhiteSpace("image_addr", ImageAddress?.NullIfDefault()?.ToHexString());
        writer.WriteNumberIfNotNull("image_size", ImageSize);
        writer.WriteStringIfNotWhiteSpace("debug_id", DebugId);
        writer.WriteStringIfNotWhiteSpace("debug_checksum", DebugChecksum);
        writer.WriteStringIfNotWhiteSpace("debug_file", DebugFile);
        writer.WriteStringIfNotWhiteSpace("code_id", CodeId);
        writer.WriteStringIfNotWhiteSpace("code_file", CodeFile);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static DebugImage FromJson(JsonElement json)
    {
        var type = json.GetPropertyOrNull("type")?.GetString();
        var imageAddress = json.GetPropertyOrNull("image_addr")?.GetHexAsLong();
        var imageSize = json.GetPropertyOrNull("image_size")?.GetInt64();
        var debugId = json.GetPropertyOrNull("debug_id")?.GetString();
        var debugChecksum = json.GetPropertyOrNull("debug_checksum")?.GetString();
        var debugFile = json.GetPropertyOrNull("debug_file")?.GetString();
        var codeId = json.GetPropertyOrNull("code_id")?.GetString();
        var codeFile = json.GetPropertyOrNull("code_file")?.GetString();

        return new DebugImage
        {
            Type = type,
            ImageAddress = imageAddress,
            ImageSize = imageSize,
            DebugId = debugId,
            DebugChecksum = debugChecksum,
            DebugFile = debugFile,
            CodeId = codeId,
            CodeFile = codeFile,
        };
    }
}
