namespace ELFSharp.ELF.Sections
{
    internal interface INoteSection : ISection
    {
        public string NoteName { get; }
        public byte[] Description { get; }
    }
}
