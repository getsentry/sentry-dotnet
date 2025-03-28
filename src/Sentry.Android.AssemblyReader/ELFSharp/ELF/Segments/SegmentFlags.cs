using System;

namespace ELFSharp.ELF.Segments
{
    [Flags]
    internal enum SegmentFlags : uint
    {
        Execute = 1,
        Write = 2,
        Read = 4
    }
}
