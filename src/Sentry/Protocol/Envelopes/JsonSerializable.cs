using Sentry.Extensibility;

namespace Sentry.Protocol.Envelopes;

/// <summary>
/// Represents an object serializable in JSON format.
/// </summary>
internal sealed class JsonSerializable : ISerializable
{
    /// <summary>
    /// Source object.
    /// </summary>
    public ISentryJsonSerializable Source { get; }

    /// <summary>
    /// Initializes an instance of <see cref="JsonSerializable"/>.
    /// </summary>
    public JsonSerializable(ISentryJsonSerializable source) => Source = source;

    /// <inheritdoc />
    public async Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default)
    {
        var writer = new Utf8JsonWriter(stream);

        await using (writer.ConfigureAwait(false))
        {
            Source.WriteTo(writer, logger);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Serialize(Stream stream, IDiagnosticLogger? logger)
    {
        using var writer = new Utf8JsonWriter(stream);
        Source.WriteTo(writer, logger);
        writer.Flush();
    }
}
