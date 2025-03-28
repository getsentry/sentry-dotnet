namespace ELFSharp.ELF.Sections
{
    internal interface INoteSection : ISection
    {
        string NoteName { get; }
        byte[] Description { get; }
    }
}