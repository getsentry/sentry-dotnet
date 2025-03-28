using System;
using System.IO;

namespace ELFSharp.Utilities
{
    internal sealed class SubStream : Stream
    {
        private const string NegativeArgumentMessage = "The argument cannot be negative.";
        private const string OutsideStreamMessage = "The argument must be within the wrapped stream.";
        private readonly long startingPosition;

        private readonly Stream wrappedStream;

        public SubStream(Stream wrappedStream, long startingPosition, long length)
        {
            if (startingPosition < 0) throw new ArgumentException(nameof(startingPosition), NegativeArgumentMessage);

            if (startingPosition > wrappedStream.Length)
                throw new ArgumentException(nameof(startingPosition), OutsideStreamMessage);

            if (length < 0) throw new ArgumentException(nameof(length), NegativeArgumentMessage);

            if (startingPosition + length > wrappedStream.Length)
                throw new ArgumentException(nameof(startingPosition), OutsideStreamMessage);

            if (!wrappedStream.CanSeek)
                throw new ArgumentException(nameof(wrappedStream), "Wrapped streem has to be seekable.");
            ;
            this.wrappedStream = wrappedStream;
            this.startingPosition = startingPosition;
            this.Length = length;

            wrappedStream.Seek(startingPosition, SeekOrigin.Begin);
        }

        public override bool CanRead => wrappedStream.CanRead;

        public override bool CanSeek => wrappedStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position
        {
            get => wrappedStream.Position - startingPosition;

            set => wrappedStream.Position = value + startingPosition;
        }

        public override void Flush()
        {
            wrappedStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, Length - Position);
            return wrappedStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // All offsets are adjusted to represent a begin-based offset in
            // the original stream, so that we can simplify sanity checks.

            var adjustedOffset = origin switch
            {
                SeekOrigin.Begin => offset + startingPosition,
                SeekOrigin.End => wrappedStream.Length - offset,
                SeekOrigin.Current => wrappedStream.Position + offset,
                _ => throw new InvalidOperationException("Should never reach here.")
            };

            if (adjustedOffset < startingPosition || adjustedOffset > startingPosition + Length)
                throw new ArgumentException(nameof(offset), "Effective offset cannot move outside of the substream.");

            return wrappedStream.Seek(adjustedOffset, SeekOrigin.Begin) - startingPosition;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException($"Setting length is not available for {nameof(SubStream)}.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException($"Writing is not available for {nameof(SubStream)}.");
        }
    }
}