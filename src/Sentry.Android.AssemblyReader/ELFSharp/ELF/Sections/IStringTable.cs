using System.Collections.Generic;

namespace ELFSharp.ELF.Sections
{
    internal interface IStringTable : ISection
    {
        public string this[long index] { get; }
        public IEnumerable<string> Strings { get; }
    }
}
