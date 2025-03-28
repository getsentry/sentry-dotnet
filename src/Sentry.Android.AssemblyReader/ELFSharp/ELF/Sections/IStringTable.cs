using System.Collections.Generic;

namespace ELFSharp.ELF.Sections
{
    internal interface IStringTable : ISection
    {
        string this[long index] { get; }
        IEnumerable<string> Strings { get; }
    }
}