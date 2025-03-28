using System;
using System.Collections.Generic;
using ELFSharp.ELF.Sections;
using ELFSharp.ELF.Segments;

namespace ELFSharp.ELF
{
    internal interface IELF : IDisposable
    {
        Endianess Endianess { get; }
        Class Class { get; }
        FileType Type { get; }
        Machine Machine { get; }
        bool HasSegmentHeader { get; }
        bool HasSectionHeader { get; }
        bool HasSectionsStringTable { get; }
        IReadOnlyList<ISegment> Segments { get; }
        IStringTable SectionsStringTable { get; }
        IReadOnlyList<ISection> Sections { get; }
        IEnumerable<T> GetSections<T>() where T : ISection;
        bool TryGetSection(string name, out ISection section);
        ISection GetSection(string name);
        bool TryGetSection(int index, out ISection section);
        ISection GetSection(int index);
    }
}
