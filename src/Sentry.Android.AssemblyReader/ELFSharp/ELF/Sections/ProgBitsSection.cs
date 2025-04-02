using System;
using ELFSharp.Utilities;

namespace ELFSharp.ELF.Sections
{
    internal sealed class ProgBitsSection<T> : Section<T>, IProgBitsSection where T : struct
    {
        private const int BufferSize = 10 * 1024;

        internal ProgBitsSection(SectionHeader header, SimpleEndianessAwareReader reader) : base(header, reader)
        {
        }


        public void WriteContents(byte[] destination, int offset, int length = 0)
        {
            SeekToSectionBeginning();
            if (length == 0 || (ulong)length > Header.Size)
                length = Convert.ToInt32(Header.Size);
            var remaining = length;
            while (remaining > 0)
            {
                var buffer = Reader.ReadBytes(Math.Min(BufferSize, remaining));
                buffer.CopyTo(destination, offset + (length - remaining));
                remaining -= buffer.Length;
            }
        }
    }
}
