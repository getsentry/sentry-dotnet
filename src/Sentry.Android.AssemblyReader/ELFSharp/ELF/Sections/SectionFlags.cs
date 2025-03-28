using System;

namespace ELFSharp.ELF.Sections
{
    [Flags]
    internal enum SectionFlags
    {
        Writable = 1,
        Allocatable = 2,
        Executable = 4
    }
}