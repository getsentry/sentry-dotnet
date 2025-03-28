namespace ELFSharp.ELF.Sections
{
    internal interface ISymbolEntry
    {
        string Name { get; }
        SymbolBinding Binding { get; }
        SymbolType Type { get; }
        SymbolVisibility Visibility { get; }
        bool IsPointedIndexSpecial { get; }
        ISection PointedSection { get; }
        ushort PointedSectionIndex { get; }
    }
}