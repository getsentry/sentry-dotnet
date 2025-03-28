using System.Collections.Generic;

namespace ELFSharp.ELF.Sections
{
    internal interface IDynamicSection : ISection
    {
        public IEnumerable<IDynamicEntry> Entries { get; }
    }
}
