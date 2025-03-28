using System.IO;
using ELFSharp.Utilities;

namespace ELFSharp.MachO
{
    internal class IdDylib : Dylib
    {
        public IdDylib(SimpleEndianessAwareReader reader, Stream stream, uint commandSize) : base(reader, stream,
            commandSize)
        {
        }
    }
}
