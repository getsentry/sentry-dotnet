namespace ELFSharp.ELF.Sections
{
    internal interface ISymbolEntry
    {
        public string Name { get; }
        public SymbolBinding Binding { get; }
        public SymbolType Type { get; }
        public SymbolVisibility Visibility { get; }
        public bool IsPointedIndexSpecial { get; }
        public ISection PointedSection { get; }
        public ushort PointedSectionIndex { get; }
    }
}
