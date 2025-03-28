using System.IO;

namespace ELFSharp.Utilities
{
    internal static class Extensions
    {
        public static byte[] ReadBytesOrThrow(this Stream stream, int count)
        {
            var result = new byte[count];
            while (count > 0)
            {
                var readThisTurn = stream.Read(result, result.Length - count, count);
                if (readThisTurn == 0)
                    throw new EndOfStreamException($"End of stream reached while {count} bytes more expected.");
                count -= readThisTurn;
            }

            return result;
        }
    }
}