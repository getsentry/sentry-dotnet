namespace ELFSharp.ELF.Sections
{
    internal interface ISection
    {
        string Name { get; }
        uint NameIndex { get; }
        SectionType Type { get; }
        SectionFlags Flags { get; }
        byte[] GetContents();
    }
}