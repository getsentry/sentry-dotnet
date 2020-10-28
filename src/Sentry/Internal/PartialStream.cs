using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Internal
{
    internal class PartialStream : Stream
    {
        // Note: currently, we don't guarantee ownership of the stream,
        // so if other consumers also read from it, we may enter undefined state.
        // Since all of this is internal, it's probably okay, but we should probably
        // put safeguards in the future.
        private readonly Stream _innerStream;
        private readonly long _offset;
        private readonly long? _length; // null means "until EOS"

        private long _position;

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _length ?? _innerStream.Length - _offset;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0 || (_length != null && value > _length.Value))
                {
                    throw new InvalidOperationException("Invalid position.");
                }

                _position = value;
            }
        }

        public PartialStream(Stream innerStream, long offset, long? length)
        {
            _innerStream = innerStream;
            _offset = offset;
            _length = length;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Make sure we don't read beyond allowed range
            var actualCount = _length != null
                ? (int) Math.Min(count, _length.Value - Position)
                : count;

            if (actualCount <= 0)
            {
                return 0;
            }

            // Synchronize inner stream's position with our own
            var innerPosition = _offset + Position;
            if (_innerStream.Position != innerPosition)
            {
                _innerStream.Position = innerPosition;
            }

            var read = await _innerStream.ReadAsync(buffer, offset, actualCount, cancellationToken)
                .ConfigureAwait(false);

            if (_length != null)
            {
                read = (int) Math.Min(read, _length.Value - Position);
            }

            Position += read;

            return read;
        }

        // This may put the position in an invalid state, but it will be validated later during read
        public override long Seek(long offset, SeekOrigin origin) => origin switch
        {
            SeekOrigin.Begin => Position = offset,
            SeekOrigin.Current => Position += offset,
            SeekOrigin.End => Position = Length - offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        [ExcludeFromCodeCoverage]
        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        [ExcludeFromCodeCoverage]
        public override void SetLength(long value) =>
            throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        [ExcludeFromCodeCoverage]
        public override void Flush() => _innerStream.Flush();
    }
}
