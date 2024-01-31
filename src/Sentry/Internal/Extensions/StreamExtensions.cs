namespace Sentry.Internal.Extensions;

internal static class StreamExtensions
{
    public static async Task<byte[]> ReadLineAsync(
        this Stream stream,
        CancellationToken cancellationToken = default)
    {
        // This approach avoids reading one byte at a time.

        const int size = 128;
        using var buffer = new PooledBuffer<byte>(size);

        using var result = new MemoryStream(capacity: size);

        var overreach = 0;
        var found = false;
        while (!found)
        {
            var bytesRead = await stream.ReadAsync(buffer.Array, 0, size, cancellationToken).ConfigureAwait(false);
            if (bytesRead <= 0)
            {
                break;
            }

            for (var i = 0; i < bytesRead; i++)
            {
                if (buffer.Array[i] != '\n')
                {
                    continue;
                }

                found = true;
                overreach = bytesRead - i - 1;
                bytesRead = i;
                break;
            }

            result.Write(buffer.Array, 0, bytesRead);
        }

        stream.Position -= overreach;
        return result.ToArray();
    }

    public static async Task SkipNewlinesAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        // We probably have very few newline characters to skip, so reading one byte at a time is fine here.

        using var buffer = new PooledBuffer<byte>(1);

        while (await stream.ReadAsync(buffer.Array, 0, 1, cancellationToken).ConfigureAwait(false) > 0)
        {
            if (buffer.Array[0] != '\n')
            {
                stream.Position--;
                return;
            }
        }
    }

    public static async Task<byte[]> ReadByteChunkAsync(
        this Stream stream,
        int expectedLength,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new PooledBuffer<byte>(expectedLength);
        var bytesRead = await stream.ReadAsync(buffer.Array, 0, expectedLength, cancellationToken)
            .ConfigureAwait(false);

        // The buffer is rented so we can't return it, plus it may be larger than needed.
        // So we copy everything to a new buffer.
        var result = new byte[bytesRead];
        Array.Copy(buffer.Array, result, bytesRead);

        return result;
    }

    // pre-creating this buffer leads to an optimized path when writing
    private static readonly byte[] NewlineBuffer = { (byte)'\n' };

    public static Task WriteNewlineAsync(this Stream stream, CancellationToken cancellationToken = default) =>
#pragma warning disable CA1835 // the byte-array implementation of WriteAsync is more direct than using ReadOnlyMemory<byte>
        stream.WriteAsync(NewlineBuffer, 0, 1, cancellationToken);
#pragma warning restore CA1835

    public static void WriteNewline(this Stream stream) => stream.Write(NewlineBuffer, 0, 1);

    public static long? TryGetLength(this Stream stream)
    {
        try
        {
            return stream.Length;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsFileStream(this Stream? stream) =>
        stream is FileStream || stream?.GetType().Name == "MockFileStream";
}
