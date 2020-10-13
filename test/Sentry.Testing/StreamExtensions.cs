using System.IO;
using System.Text;

namespace Sentry.Testing
{
    public static class StreamExtensions
    {
        public static MemoryStream ToMemoryStream(this string source, Encoding encoding) =>
            new MemoryStream(encoding.GetBytes(source));

        public static MemoryStream ToMemoryStream(this string source) =>
            source.ToMemoryStream(Encoding.UTF8);
    }
}
