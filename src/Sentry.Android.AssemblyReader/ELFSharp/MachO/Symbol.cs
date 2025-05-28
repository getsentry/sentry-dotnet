using System.Diagnostics;

namespace ELFSharp.MachO
{
    [DebuggerDisplay("Symbol({Name,nq},{Value}) in {Section}")]
    internal struct Symbol
    {
        public Symbol(string name, long value, Section section) : this()
        {
            Name = name;
            Value = value;
            Section = section;
        }

        public string Name { get; private set; }
        public long Value { get; private set; }
        public Section Section { get; private set; }
    }
}
