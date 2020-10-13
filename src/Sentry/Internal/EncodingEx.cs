using System.Text;

namespace Sentry.Internal
{
    internal static class EncodingEx
    {
        public static Encoding Utf8WithoutBom { get; } = new UTF8Encoding(false, true);
    }
}
