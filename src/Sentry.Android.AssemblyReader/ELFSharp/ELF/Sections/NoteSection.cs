using ELFSharp.Utilities;

namespace ELFSharp.ELF.Sections
{
    internal sealed class NoteSection<T> : Section<T>, INoteSection where T : struct
    {
        private readonly NoteData data;

        internal NoteSection(SectionHeader header, SimpleEndianessAwareReader reader) : base(header, reader)
        {
            data = new NoteData(header.Offset, header.Size, reader);
        }

        public T NoteType => data.Type.To<T>();

        public string NoteName => data.Name;

        public byte[] Description => data.DescriptionBytes;

        public override string ToString()
        {
            return string.Format("{0}: {2}, Type={1}", Name, NoteType, Type);
        }
    }
}
