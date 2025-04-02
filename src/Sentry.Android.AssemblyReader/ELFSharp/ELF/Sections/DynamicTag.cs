namespace ELFSharp.ELF.Sections
{
    /// <summary>
    ///     This enum holds some of the possible values for the DynamicTag value (dropping platform
    ///     specific contents, such as MIPS flags.)
    ///     Values are coming from LLVM's elf.h headers.
    ///     File can be found in LLVM 3.8.1 source at:
    ///     ../include/llvm/support/elf.h
    ///     License of the original C code is LLVM license.
    /// </summary>
    internal enum DynamicTag : ulong
    {
        Null = 0, // Marks end of dynamic array.
        Needed = 1, // String table offset of needed library.
        PLTRelSz = 2, // Size of relocation entries in PLT.
        PLTGOT = 3, // Address associated with linkage table.
        Hash = 4, // Address of symbolic hash table.
        StrTab = 5, // Address of dynamic string table.
        SymTab = 6, // Address of dynamic symbol table.
        RelA = 7, // Address of relocation table (Rela entries).
        RelASz = 8, // Size of Rela relocation table.
        RelAEnt = 9, // Size of a Rela relocation entry.
        StrSz = 10, // Total size of the string table.
        SymEnt = 11, // Size of a symbol table entry.
        Init = 12, // Address of initialization function.
        Fini = 13, // Address of termination function.
        SoName = 14, // String table offset of a shared objects name.
        RPath = 15, // String table offset of library search path.
        Symbolic = 16, // Changes symbol resolution algorithm.
        Rel = 17, // Address of relocation table (Rel entries).
        RelSz = 18, // Size of Rel relocation table.
        RelEnt = 19, // Size of a Rel relocation entry.
        PLTRel = 20, // Type of relocation entry used for linking.
        Debug = 21, // Reserved for debugger.
        TextRel = 22, // Relocations exist for non-writable segments.
        JmpRel = 23, // Address of relocations associated with PLT.
        BindNow = 24, // Process all relocations before execution.
        InitArray = 25, // Pointer to array of initialization functions.
        FiniArray = 26, // Pointer to array of termination functions.
        InitArraySz = 27, // Size of DT_INIT_ARRAY.
        FiniArraySz = 28, // Size of DT_FINI_ARRAY.
        RunPath = 29, // String table offset of lib search path.
        Flags = 30, // Flags.
        Encoding = 32, // Values from here to DT_LOOS follow the rules for the interpretation of the d_un union.

        PreInitArray = 32, // Pointer to array of preinit functions.
        PreInitArraySz = 33, // Size of the DT_PREINIT_ARRAY array.
        LoOS = 0x60000000, // Start of environment specific tags.
        HiOS = 0x6FFFFFFF, // End of environment specific tags.
        LoProc = 0x70000000, // Start of processor specific tags.
        HiProc = 0x7FFFFFFF, // End of processor specific tags.
        GNUHash = 0x6FFFFEF5, // Reference to the GNU hash table.
        TLSDescPLT = 0x6FFFFEF6, // Location of PLT entry for TLS descriptor resolver calls.
        TLSDescGOT = 0x6FFFFEF7, // Location of GOT entry used by TLS descriptor resolver PLT entry.
        RelACount = 0x6FFFFFF9, // ELF32_Rela count.
        RelCount = 0x6FFFFFFA, // ELF32_Rel count.
        Flags1 = 0X6FFFFFFB, // Flags_1.
        VerSym = 0x6FFFFFF0, // The address of .gnu.version section.
        VerDef = 0X6FFFFFFC, // The address of the version definition table.
        VerDefNum = 0X6FFFFFFD, // The number of entries in DT_VERDEF.
        VerNeed = 0X6FFFFFFE, // The address of the version Dependency table.
        VerNeedNum = 0X6FFFFFFF // The number of entries in DT_VERNEED.
    }
}
