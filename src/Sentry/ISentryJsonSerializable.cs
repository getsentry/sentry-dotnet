using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Sentry JsonSerializable.
/// </summary>
public interface ISentryJsonSerializable
{
    /// <summary>
    /// Writes the object as JSON.
    /// </summary>
    /// <remarks>
    /// Note: this method is meant only for internal use and is exposed due to a language limitation.
    /// Avoid relying on this method in user code.
    /// </remarks>
    void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger);
}

internal static class JsonSerializableExtensions
{
    public static void WriteToFile(this ISentryJsonSerializable serializable, ISentryFileSystem fileSystem, string filePath, IDiagnosticLogger? logger)
    {
        using var file = fileSystem.CreateFileForWriting(filePath);
        if (file == Stream.Null)
        {
            return;
        }

        using var writer = new Utf8JsonWriter(file);

        serializable.WriteTo(writer, logger);
        writer.Flush();
    }
}
