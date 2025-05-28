namespace ELFSharp.ELF.Sections
{
    internal enum SpecialSectionIndex : ushort
    {
        Absolute = 0,
        Common = 0xFFF1,
        Undefined = 0xFFF2
    }
}
