using System.Collections.Generic;
using System.Collections.ObjectModel;
using ELFSharp.Utilities;

#nullable disable

namespace ELFSharp.ELF.Sections
{
    internal sealed class SymbolTable<T> : Section<T>, ISymbolTable where T : struct
    {
        private readonly ELF<T> elf;
        private readonly IStringTable table;

        private List<SymbolEntry<T>> entries;

        internal SymbolTable(SectionHeader header, SimpleEndianessAwareReader Reader, IStringTable table, ELF<T> elf) :
            base(header, Reader)
        {
            this.table = table;
            this.elf = elf;
            ReadSymbols();
        }

        public IEnumerable<SymbolEntry<T>> Entries => new ReadOnlyCollection<SymbolEntry<T>>(entries);

        IEnumerable<ISymbolEntry> ISymbolTable.Entries => Entries;

        private void ReadSymbols()
        {
            SeekToSectionBeginning();
            entries = new List<SymbolEntry<T>>();
            var adder = (ulong)(elf.Class == Class.Bit32 ? Consts.SymbolEntrySize32 : Consts.SymbolEntrySize64);
            for (var i = 0UL; i < Header.Size; i += adder)
            {
                var value = 0UL;
                var size = 0UL;
                var nameIdx = Reader.ReadUInt32();

                if (elf.Class == Class.Bit32)
                {
                    value = Reader.ReadUInt32();
                    size = Reader.ReadUInt32();
                }

                var info = Reader.ReadByte();
                var other = Reader.ReadByte();
                var visibility = (SymbolVisibility)(other & 3); // Only three lowest bits are meaningful.
                var sectionIdx = Reader.ReadUInt16();

                if (elf.Class == Class.Bit64)
                {
                    value = Reader.ReadUInt64();
                    size = Reader.ReadUInt64();
                }

                var name = table == null ? "<corrupt>" : table[nameIdx];
                var binding = (SymbolBinding)(info >> 4);
                var type = (SymbolType)(info & 0x0F);
                entries.Add(new SymbolEntry<T>(name, value.To<T>(), size.To<T>(), visibility, binding, type, elf,
                    sectionIdx));
            }
        }
    }
}
