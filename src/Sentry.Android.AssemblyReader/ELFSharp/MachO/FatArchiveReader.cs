using System.Collections.Generic;
using System.IO;
using ELFSharp.Utilities;

namespace ELFSharp.MachO
{
    internal static class FatArchiveReader
    {
        public static IEnumerable<MachO> Enumerate(Stream stream, bool shouldOwnStream)
        {
            // Fat header is always big endian.
            var reader = new SimpleEndianessAwareReader(stream, Endianess.BigEndian, !shouldOwnStream);

            // We assume that fat magic has been already read.
            var machOCount = reader.ReadInt32();
            var alreadyRead = 0;
            var fatEntriesBegin = stream.Position;

            while (alreadyRead < machOCount)
            {
                // We're only interested in offset and size.
                stream.Seek(fatEntriesBegin + 20 * alreadyRead + 8, SeekOrigin.Begin);
                var offset = reader.ReadInt32();
                var size = reader.ReadInt32();
                var substream = new SubStream(stream, offset, size);
                yield return MachOReader.Load(substream, false);

                alreadyRead++;
            }
        }
    }
}