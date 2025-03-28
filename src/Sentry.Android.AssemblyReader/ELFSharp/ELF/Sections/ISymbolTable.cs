using System.Collections.Generic;

namespace ELFSharp.ELF.Sections
{
    internal interface ISymbolTable : ISection
    {
        public IEnumerable<ISymbolEntry> Entries { get; }
    }
}
