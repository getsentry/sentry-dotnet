using System;
using System.IO;
using ELFSharp.Utilities;

#nullable disable

namespace ELFSharp.ELF.Segments
{
    internal class Segment<T> : ISegment
    {
        private readonly Class elfClass;

        private readonly long headerOffset;
        private readonly SimpleEndianessAwareReader reader;

        internal Segment(long headerOffset, Class elfClass, SimpleEndianessAwareReader reader)
        {
            this.reader = reader;
            this.headerOffset = headerOffset;
            this.elfClass = elfClass;
            ReadHeader();
        }

        public T Address { get; private set; }

        public T PhysicalAddress { get; private set; }

        public T Size { get; private set; }

        public T Alignment { get; private set; }

        public long FileSize { get; private set; }

        public long Offset { get; private set; }

        public SegmentType Type { get; private set; }

        public SegmentFlags Flags { get; private set; }

        /// <summary>
        ///     Returns content of the section as it is given in the file.
        ///     Note that it may be an array of length 0.
        /// </summary>
        /// <returns>Segment contents as byte array.</returns>
        public byte[] GetFileContents()
        {
            if (FileSize == 0) return new byte[0];

            SeekTo(Offset);
            var result = new byte[checked((int)FileSize)];
            var fileImage = reader.ReadBytes(result.Length);
            fileImage.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        ///     Returns content of the section, possibly padded or truncated to the memory size.
        ///     Note that it may be an array of length 0.
        /// </summary>
        /// <returns>Segment image as a byte array.</returns>
        public byte[] GetMemoryContents()
        {
            var sizeAsInt = Size.To<int>();
            if (sizeAsInt == 0) return new byte[0];

            SeekTo(Offset);
            var result = new byte[sizeAsInt];
            var fileImage = reader.ReadBytes(Math.Min(result.Length, checked((int)FileSize)));
            fileImage.CopyTo(result, 0);
            return result;
        }

        public byte[] GetRawHeader()
        {
            SeekTo(headerOffset);
            return reader.ReadBytes(elfClass == Class.Bit32 ? 32 : 56);
        }

        public static SegmentType ProbeType(SimpleEndianessAwareReader reader)
        {
            return (SegmentType)reader.ReadUInt32();
        }

        public override string ToString()
        {
            return string.Format("{2}: size {3}, @ 0x{0:X}", Address, PhysicalAddress, Type, Size);
        }

        private void ReadHeader()
        {
            SeekTo(headerOffset);
            Type = (SegmentType)reader.ReadUInt32();
            if (elfClass == Class.Bit64) Flags = (SegmentFlags)reader.ReadUInt32();
            // TODO: some functions?s
            Offset = elfClass == Class.Bit32 ? reader.ReadUInt32() : reader.ReadInt64();
            Address = (elfClass == Class.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
            PhysicalAddress = (elfClass == Class.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
            FileSize = elfClass == Class.Bit32 ? reader.ReadInt32() : reader.ReadInt64();
            Size = (elfClass == Class.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
            if (elfClass == Class.Bit32) Flags = (SegmentFlags)reader.ReadUInt32();

            Alignment = (elfClass == Class.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
        }

        private void SeekTo(long givenOffset)
        {
            reader.BaseStream.Seek(givenOffset, SeekOrigin.Begin);
        }
    }
}
