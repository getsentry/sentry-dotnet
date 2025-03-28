using System.IO;
using ELFSharp.Utilities;

namespace ELFSharp.MachO
{
    internal class Command
    {
        protected readonly SimpleEndianessAwareReader Reader;
        protected readonly Stream Stream;

        internal Command(SimpleEndianessAwareReader reader, Stream stream)
        {
            Stream = stream;
            Reader = reader;
        }
    }
}
