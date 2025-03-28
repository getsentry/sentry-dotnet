using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ELFSharp.Utilities;

#nullable disable

namespace ELFSharp.MachO
{
    internal class SymbolTable : Command
    {
        private readonly bool is64;

        private Symbol[] symbols;

        public SymbolTable(SimpleEndianessAwareReader reader, Stream stream, bool is64, IReadOnlyList<Section> sections)
            : base(reader, stream)
        {
            this.is64 = is64;
            ReadSymbols(sections);
        }

        public IEnumerable<Symbol> Symbols
        {
            get { return symbols.Select(x => x); }
        }

        private void ReadSymbols(IReadOnlyList<Section> sections)
        {
            var symbolTableOffset = Reader.ReadInt32();
            var numberOfSymbols = Reader.ReadInt32();
            symbols = new Symbol[numberOfSymbols];
            var stringTableOffset = Reader.ReadInt32();
            Reader.ReadInt32(); // string table size

            var streamPosition = Stream.Position;
            Stream.Seek(symbolTableOffset, SeekOrigin.Begin);

            try
            {
                for (var i = 0; i < numberOfSymbols; i++)
                {
                    var nameOffset = Reader.ReadInt32();
                    var name = ReadStringFromOffset(stringTableOffset + nameOffset);
                    var type = Reader.ReadByte();
                    var sect = Reader.ReadByte();
                    var desc = Reader.ReadInt16();
                    var value = is64 ? Reader.ReadInt64() : Reader.ReadInt32();
                    var symbol = new Symbol(name, value,
                        sect > 0 && sect <= sections.Count ? sections[sect - 1] : null);
                    symbols[i] = symbol;
                }
            }
            finally
            {
                Stream.Position = streamPosition;
            }
        }

        private string ReadStringFromOffset(int offset)
        {
            var streamPosition = Stream.Position;
            Stream.Seek(offset, SeekOrigin.Begin);
            try
            {
                var asBytes = new List<byte>();
                int readByte;
                while ((readByte = Stream.ReadByte()) != 0)
                {
                    if (readByte == -1)
                        throw new EndOfStreamException("Premature end of the stream while reading string.");
                    asBytes.Add((byte)readByte);
                }

                return Encoding.UTF8.GetString(asBytes.ToArray());
            }
            finally
            {
                Stream.Position = streamPosition;
            }
        }
    }
}
