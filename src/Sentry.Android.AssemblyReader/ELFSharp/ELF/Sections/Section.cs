using System;
using System.IO;
using ELFSharp.Utilities;

namespace ELFSharp.ELF.Sections
{
    internal class Section<T> : ISection where T : struct
    {
        protected readonly SimpleEndianessAwareReader Reader;

        internal Section(SectionHeader header, SimpleEndianessAwareReader reader)
        {
            Header = header;
            Reader = reader;
        }

        public T RawFlags => Header.RawFlags.To<T>();

        public T LoadAddress => Header.LoadAddress.To<T>();

        public T Alignment => Header.Alignment.To<T>();

        public T EntrySize => Header.EntrySize.To<T>();

        public T Size => Header.Size.To<T>();

        public T Offset => Header.Offset.To<T>();

        internal SectionHeader Header { get; }

        public virtual byte[] GetContents()
        {
            if (Type == SectionType.NoBits) return Array.Empty<byte>();

            Reader.BaseStream.Seek((long)Header.Offset, SeekOrigin.Begin);
            return Reader.ReadBytes(Convert.ToInt32(Header.Size));
        }

        public string Name => Header.Name;

        public uint NameIndex => Header.NameIndex;

        public SectionType Type => Header.Type;

        public SectionFlags Flags => Header.Flags;

        public override string ToString()
        {
            return Header.ToString();
        }

        protected void SeekToSectionBeginning()
        {
            Reader.BaseStream.Seek((long)Header.Offset, SeekOrigin.Begin);
        }
    }
}