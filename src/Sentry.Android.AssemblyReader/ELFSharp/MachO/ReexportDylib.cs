using System.IO;
using ELFSharp.Utilities;

namespace ELFSharp.MachO
{
    internal class ReexportDylib : Dylib
    {
        public ReexportDylib(SimpleEndianessAwareReader reader, Stream stream, uint commandSize) : base(reader, stream,
            commandSize)
        {
        }
    }
}
