namespace ELFSharp.ELF.Segments
{
    internal enum SegmentType : uint
    {
        Null = 0,
        Load,
        Dynamic,
        Interpreter,
        Note,
        SharedLibrary,
        ProgramHeader
    }
}
