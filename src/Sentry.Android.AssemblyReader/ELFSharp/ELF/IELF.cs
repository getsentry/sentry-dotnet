using System;
using System.Collections.Generic;
using ELFSharp.ELF.Sections;
using ELFSharp.ELF.Segments;

namespace ELFSharp.ELF
{
    internal interface IELF : IDisposable
    {
        public Endianess Endianess { get; }
        public Class Class { get; }
        public FileType Type { get; }
        public Machine Machine { get; }
        public bool HasSegmentHeader { get; }
        public bool HasSectionHeader { get; }
        public bool HasSectionsStringTable { get; }
        public IReadOnlyList<ISegment> Segments { get; }
        public IStringTable SectionsStringTable { get; }
        public IReadOnlyList<ISection> Sections { get; }
        public IEnumerable<T> GetSections<T>() where T : ISection;
        public bool TryGetSection(string name, out ISection section);
        public ISection GetSection(string name);
        public bool TryGetSection(int index, out ISection section);
        public ISection GetSection(int index);
    }
}
