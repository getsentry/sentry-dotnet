using System.IO.Compression;

namespace Sentry.Internal;

/// <summary>
/// Read-only, non-seekable stream that gzip-compresses a source stream as it is read.
/// Nothing is read from the source until the first <see cref="Read"/>, so the compression
/// runs on whichever thread consumes the stream rather than the one that created it.
/// </summary>
internal sealed class GzipReadStream : Stream
{
    private const int ChunkSize = 81920;

    private readonly Stream _source;
    private readonly MemoryStream _sink = new();
    private readonly byte[] _chunk = new byte[ChunkSize];
    private GZipStream? _gzip;
    private int _sinkPosition;

    public GzipReadStream(Stream source, CompressionLevel compressionLevel)
    {
        _source = source;
        _gzip = new GZipStream(_sink, compressionLevel, leaveOpen: true);
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        while (_sinkPosition == _sink.Length)
        {
            if (!CompressNextChunk())
            {
                return 0;
            }
        }

        var available = (int)_sink.Length - _sinkPosition;
        var length = Math.Min(count, available);
        Buffer.BlockCopy(_sink.GetBuffer(), _sinkPosition, buffer, offset, length);
        _sinkPosition += length;
        return length;
    }

    /// <summary>
    /// Feeds the next source chunk to the compressor. The deflater may buffer a chunk without
    /// emitting anything, so the sink can still be empty afterwards.
    /// </summary>
    /// <returns><c>false</c> once the source is exhausted and the gzip trailer has been emitted.</returns>
    private bool CompressNextChunk()
    {
        if (_gzip is null)
        {
            return false;
        }

        _sink.SetLength(0);
        _sinkPosition = 0;

        var read = _source.Read(_chunk, 0, _chunk.Length);
        if (read > 0)
        {
            _gzip.Write(_chunk, 0, read);
        }
        else
        {
            // Dispose the compressor to flush the gzip trailer
            _gzip.Dispose();
            _gzip = null;
        }

        return true;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gzip?.Dispose();
            _sink.Dispose();
            _source.Dispose();
        }

        base.Dispose(disposing);
    }
}
