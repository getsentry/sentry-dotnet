using Sentry.Extensibility;

namespace Sentry.Protocol.Envelopes;

/// <summary>
/// Represents a task producing an object serializable to JSON format.
/// </summary>
internal sealed class AsyncJsonSerializable : ISerializable
{
    /// <summary>
    /// Source object.
    /// </summary>
    public Task<ISentryJsonSerializable> Source { get; }

    /// <summary>
    /// Initializes an instance of <see cref="AsyncJsonSerializable"/>.
    /// </summary>
    public static AsyncJsonSerializable CreateFrom<T>(Task<T> source)
        where T : ISentryJsonSerializable
    {
        var task = source.ContinueWith(t => t.Result as ISentryJsonSerializable);
        return new AsyncJsonSerializable(task);
    }

    private AsyncJsonSerializable(Task<ISentryJsonSerializable> source) => Source = source;

    /// <inheritdoc />
    public async Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default)
    {
        var source = await Source.ConfigureAwait(false);
        var writer = new Utf8JsonWriter(stream);
        await using (writer.ConfigureAwait(false))
        {
            source.WriteTo(writer, logger);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Serialize(Stream stream, IDiagnosticLogger? logger)
    {
        using var writer = new Utf8JsonWriter(stream);
        Source.Result.WriteTo(writer, logger);
        writer.Flush();
    }
}
