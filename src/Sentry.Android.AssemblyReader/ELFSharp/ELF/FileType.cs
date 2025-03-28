namespace ELFSharp.ELF
{
    internal enum FileType : ushort
    {
        None = 0,
        Relocatable,
        Executable,
        SharedObject,
        Core
    }
}
