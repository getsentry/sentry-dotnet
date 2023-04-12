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
    public Task<IJsonSerializable?> Source { get; }

    /// <summary>
    /// Initializes an instance of <see cref="AsyncJsonSerializable"/>.
    /// </summary>
    public static AsyncJsonSerializable CreateFrom<T>(Task<T?> source)
        where T : IJsonSerializable
    {
        var task = source.ContinueWith(t => t.Result as IJsonSerializable);
        return new AsyncJsonSerializable(task);
    }

    private AsyncJsonSerializable(Task<IJsonSerializable?> source) => Source = source;

    /// <inheritdoc />
    public async Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default)
    {
        if (await Source.ConfigureAwait(false) is { } source)
        {
            var writer = new Utf8JsonWriter(stream);
            await using (writer.ConfigureAwait(false))
            {
                source.WriteTo(writer, logger);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public void Serialize(Stream stream, IDiagnosticLogger? logger)
    {
        if (Source.Result is { } source)
        {
            using var writer = new Utf8JsonWriter(stream);
            Source.Wait();
            source.WriteTo(writer, logger);
            writer.Flush();
        }
    }
}
