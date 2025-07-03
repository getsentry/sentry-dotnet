// Copied from: https://github.com/dotnet/aspnetcore/blob/c18e93a9a2e2949e1a9c880da16abf0837aa978f/src/Shared/SizeLimitedStream.cs
// The only changes are the namespace and the addition of this comment

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Sentry.AspNetCore.RequestDecompression;

#nullable enable

internal sealed class SizeLimitedStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long? _sizeLimit;
    private readonly Action<long>? _handleSizeLimit;
    private long _totalBytesRead;

    public SizeLimitedStream(Stream innerStream, long? sizeLimit, Action<long>? handleSizeLimit = null)
    {
        ArgumentNullException.ThrowIfNull(innerStream);

        _innerStream = innerStream;
        _sizeLimit = sizeLimit;
        _handleSizeLimit = handleSizeLimit;
    }

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => _innerStream.CanWrite;

    public override long Length => _innerStream.Length;

    public override long Position
    {
        get
        {
            return _innerStream.Position;
        }
        set
        {
            _innerStream.Position = value;
        }
    }

    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);

        _totalBytesRead += bytesRead;
        if (_totalBytesRead > _sizeLimit)
        {
            if (_handleSizeLimit != null)
            {
                _handleSizeLimit(_sizeLimit.Value);
            }
            else
            {
                throw new InvalidOperationException("The maximum number of bytes have been read.");
            }
        }

        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
#pragma warning disable CA2007
        var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);
#pragma warning restore CA2007

        _totalBytesRead += bytesRead;
        if (_totalBytesRead > _sizeLimit)
        {
            if (_handleSizeLimit != null)
            {
                _handleSizeLimit(_sizeLimit.Value);
            }
            else
            {
                throw new InvalidOperationException("The maximum number of bytes have been read.");
            }
        }

        return bytesRead;
    }
}
