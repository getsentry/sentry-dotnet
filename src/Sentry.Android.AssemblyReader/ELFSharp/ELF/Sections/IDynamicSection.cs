using System.Collections.Generic;

namespace ELFSharp.ELF.Sections
{
    internal interface IDynamicSection : ISection
    {
        IEnumerable<IDynamicEntry> Entries { get; }
    }
}