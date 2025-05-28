using System;

#nullable disable

namespace ELFSharp.ELF.Sections
{
    internal class SymbolEntry<T> : ISymbolEntry where T : struct
    {
        private readonly ELF<T> elf;

        public SymbolEntry(string name, T value, T size, SymbolVisibility visibility,
            SymbolBinding binding, SymbolType type, ELF<T> elf, ushort sectionIdx)
        {
            Name = name;
            Value = value;
            Size = size;
            Binding = binding;
            Type = type;
            Visibility = visibility;
            this.elf = elf;
            PointedSectionIndex = sectionIdx;
        }

        public T Value { get; }

        public T Size { get; }

        public Section<T> PointedSection => IsPointedIndexSpecial ? null : elf.GetSection(PointedSectionIndex);

        public SpecialSectionIndex SpecialPointedSectionIndex
        {
            get
            {
                if (IsPointedIndexSpecial)
                    return (SpecialSectionIndex)PointedSectionIndex;
                throw new InvalidOperationException("Given pointed section index does not have special meaning.");
            }
        }

        public string Name { get; }

        public SymbolBinding Binding { get; }

        public SymbolType Type { get; }

        public SymbolVisibility Visibility { get; }

        public bool IsPointedIndexSpecial => Enum.IsDefined(typeof(SpecialSectionIndex), PointedSectionIndex);

        ISection ISymbolEntry.PointedSection => PointedSection;

        public ushort PointedSectionIndex { get; }

        public override string ToString()
        {
            return string.Format("[{3} {4} {0}: 0x{1:X}, size: {2}, section idx: {5}]",
                Name, Value, Size, Binding, Type, (SpecialSectionIndex)PointedSectionIndex);
        }
    }
}
