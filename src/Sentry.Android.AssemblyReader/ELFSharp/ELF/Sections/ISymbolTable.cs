using System.Collections.Generic;

namespace ELFSharp.ELF.Sections
{
    internal interface ISymbolTable : ISection
    {
        IEnumerable<ISymbolEntry> Entries { get; }
    }
}