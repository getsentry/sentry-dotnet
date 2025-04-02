namespace ELFSharp.MachO
{
    internal enum CommandType : uint
    {
        Segment = 0x1,
        SymbolTable = 0x2,
        LoadDylib = 0xc,
        IdDylib = 0xd,
        LoadWeakDylib = 0x80000018u,
        Segment64 = 0x19,
        ReexportDylib = 0x8000001fu,
        Main = 0x80000028u,
        UUID = 0x1b
    }
}
