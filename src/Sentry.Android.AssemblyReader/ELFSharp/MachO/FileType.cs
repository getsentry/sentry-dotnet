namespace ELFSharp.MachO
{
    internal enum FileType : uint
    {
        Object = 0x1,
        Executable = 0x2,
        FixedVM = 0x3,
        Core = 0x4,
        Preload = 0x5,
        DynamicLibrary = 0x6,
        DynamicLinker = 0x7,
        Bundle = 0x8,
        DynamicLibraryStub = 0x9,
        Debug = 0xA,
        Kext = 0xB
    }
}
