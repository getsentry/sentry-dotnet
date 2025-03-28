namespace ELFSharp.MachO
{
    internal enum Machine
    {
        Any = -1,
        Vax = 1,
        M68k = 6,
        X86 = 7,
        X86_64 = X86 | MachO.Architecture64,
        M98k = 10,
        PaRisc = 11,
        Arm = 12,
        Arm64 = Arm | MachO.Architecture64,
        M88k = 13,
        Sparc = 14,
        I860 = 15,
        PowerPc = 18,
        PowerPc64 = PowerPc | MachO.Architecture64
    }
}
