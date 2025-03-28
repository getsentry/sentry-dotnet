using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ELFSharp.Utilities;

#nullable disable

namespace ELFSharp.MachO
{
    [DebuggerDisplay("{Type}({Name,nq})")]
    internal sealed class Segment : Command
    {
        private readonly byte[] data;

        private readonly bool is64;

        public Segment(SimpleEndianessAwareReader reader, Stream stream, MachO machO) : base(reader, stream)
        {
            is64 = machO.Is64;
            Name = ReadSectionOrSegmentName();
            Address = ReadUInt32OrUInt64();
            Size = ReadUInt32OrUInt64();
            FileOffset = ReadUInt32OrUInt64();
            var fileSize = ReadUInt32OrUInt64();
            MaximalProtection = ReadProtection();
            InitialProtection = ReadProtection();
            var numberOfSections = Reader.ReadInt32();
            Reader.ReadInt32(); // we ignore flags for now

            if (fileSize > 0)
            {
                var streamPosition = Stream.Position;
                Stream.Seek((long)FileOffset, SeekOrigin.Begin);
                data = new byte[Size];
                var buffer = stream.ReadBytesOrThrow(checked((int)fileSize));
                Array.Copy(buffer, data, buffer.Length);
                Stream.Position = streamPosition;
            }

            var sections = new List<Section>();
            for (var i = 0; i < numberOfSections; i++)
            {
                var sectionName = ReadSectionOrSegmentName();
                var segmentName = ReadSectionOrSegmentName();

                // An intermediate object file contains only one segment.
                // This segment name is empty, its sections segment names are not empty.
                if (machO.FileType != FileType.Object && segmentName != Name)
                    throw new InvalidOperationException("Unexpected name of the section's segment.");

                var sectionAddress = ReadUInt32OrUInt64();
                var sectionSize = ReadUInt32OrUInt64();
                var offset = Reader.ReadUInt32();
                var alignExponent = Reader.ReadUInt32();
                var relocOffset = Reader.ReadUInt32();
                var numberOfReloc = Reader.ReadUInt32();
                var flags = Reader.ReadUInt32();
                _ = Reader.ReadUInt32(); // reserved1
                _ = Reader.ReadUInt32(); // reserved2
                _ = is64 ? Reader.ReadUInt32() : 0; // reserved3

                var section = new Section(sectionName, segmentName, sectionAddress, sectionSize, offset, alignExponent,
                    relocOffset, numberOfReloc, flags, this);
                sections.Add(section);
            }

            Sections = new ReadOnlyCollection<Section>(sections);
        }

        public string Name { get; private set; }
        public ulong Address { get; private set; }
        public ulong Size { get; }
        public ulong FileOffset { get; }
        public Protection InitialProtection { get; private set; }
        public Protection MaximalProtection { get; private set; }
        public ReadOnlyCollection<Section> Sections { get; private set; }
        private CommandType Type => is64 ? CommandType.Segment64 : CommandType.Segment;

        public byte[] GetData()
        {
            if (data == null) return new byte[Size];
            return data.ToArray();
        }

        private ulong ReadUInt32OrUInt64()
        {
            return is64 ? Reader.ReadUInt64() : Reader.ReadUInt32();
        }

        private Protection ReadProtection()
        {
            return (Protection)Reader.ReadInt32();
        }

        private string ReadSectionOrSegmentName()
        {
            var nameAsBytes = Reader.ReadBytes(16).TakeWhile(x => x != 0).ToArray();
            return Encoding.UTF8.GetString(nameAsBytes);
        }
    }
}
