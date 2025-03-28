using System;

namespace ELFSharp.MachO
{
    [Flags]
    internal enum Protection
    {
        Read = 1,
        Write = 2,
        Execute = 4
    }
}
