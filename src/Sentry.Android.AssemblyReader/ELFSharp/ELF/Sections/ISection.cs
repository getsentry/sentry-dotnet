namespace ELFSharp.ELF.Sections
{
    internal interface ISection
    {
        public string Name { get; }
        public uint NameIndex { get; }
        public SectionType Type { get; }
        public SectionFlags Flags { get; }
        public byte[] GetContents();
    }
}
