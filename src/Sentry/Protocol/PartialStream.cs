using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol
{
    internal class PartialStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _startIndex;
        private readonly long? _length;

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _length ?? throw new NotSupportedException();

        public override long Position { get; set; }

        public PartialStream(Stream innerStream, long startIndex, long? length)
        {
            _innerStream = innerStream;
            _startIndex = startIndex;
            _length = length;
        }

        // Current and inner streams' positions may be out of sync at any point
        // if they were changed from outside.
        private void SynchronizePosition()
        {
            var innerPosition = Position - _startIndex;

            // Ensure that the new position is in range
            if (innerPosition < _startIndex || (_length != null && innerPosition > _startIndex + _length.Value))
            {
                throw new InvalidOperationException("Cannot access stream outside of the allowed range.");
            }

            if (_innerStream.Position != innerPosition)
            {
                _innerStream.Position = innerPosition;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            SynchronizePosition();

            var read = _innerStream.Read(buffer, offset, count);
            Position += read;

            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            SynchronizePosition();

            var read = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            Position += read;

            return read;
        }

        // This may put the position in an invalid state, but it will be validated later during read
        public override long Seek(long offset, SeekOrigin origin) => origin switch
        {
            SeekOrigin.Begin => Position = _startIndex + offset,

            SeekOrigin.Current => Position += offset,

            SeekOrigin.End => _length != null
                ? Position = _startIndex + _length.Value - offset
                : throw new NotSupportedException(),

            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

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
