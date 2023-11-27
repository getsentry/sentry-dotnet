using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// The Sentry Debug Meta interface.
/// </summary>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/debugmeta"/>
internal sealed class DebugMeta : IJsonSerializable
{
    public List<DebugImage>? Images { get; set; }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteArrayIfNotEmpty("images", Images, logger);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static DebugMeta FromJson(JsonElement json)
    {
        var images = json.GetPropertyOrNull("images")?.EnumerateArray().Select(DebugImage.FromJson).ToList();

        return new DebugMeta
        {
            Images = images
        };
    }
}
