using System.IO;
using ELFSharp.Utilities;

namespace ELFSharp.MachO
{
    internal class EntryPoint : Command
    {
        public EntryPoint(SimpleEndianessAwareReader reader, Stream stream) : base(reader, stream)
        {
            Value = Reader.ReadInt64();
            StackSize = Reader.ReadInt64();
        }

        public long Value { get; private set; }

        public long StackSize { get; private set; }
    }
}
