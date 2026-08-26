using Sentry.Extensibility;

namespace Sentry.Protocol.Envelopes;

/// <summary>
/// Represents stream content that is buffered lazily for repeated serialization.
/// </summary>
internal sealed class BufferedStreamSerializable : ISerializable, IDisposable
{
    // Retain ownership of the source so it is disposed with the envelope item.
    private readonly Stream _source;
    private readonly Lazy<Task<byte[]>> _bufferTask;

    /// <summary>
    /// Initializes an instance of <see cref="BufferedStreamSerializable"/>.
    /// </summary>
    /// <param name="source">The source stream to buffer.</param>
    public BufferedStreamSerializable(Stream source)
    {
        _source = source;
        _bufferTask = new Lazy<Task<byte[]>>(BufferAsync);
    }

    /// <inheritdoc />
    public async Task SerializeAsync(
        Stream stream,
        IDiagnosticLogger? logger,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var buffer = await _bufferTask.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Serialize(Stream stream, IDiagnosticLogger? logger)
    {
        var buffer = _bufferTask.Value.GetAwaiter().GetResult();
        stream.Write(buffer, 0, buffer.Length);
    }

    /// <inheritdoc />
    public void Dispose() => _source.Dispose();

    private async Task<byte[]> BufferAsync()
    {
        try
        {
            using var buffer = new MemoryStream();
            await _source.CopyToAsync(buffer).ConfigureAwait(false);
            return buffer.ToArray();
        }
        catch
        {
            _source.Dispose();
            throw;
        }
    }
}
