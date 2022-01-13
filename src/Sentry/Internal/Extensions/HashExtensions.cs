using System.Security.Cryptography;
using System.Text;

namespace Sentry.Internal.Extensions
{
    internal static class HashExtensions
    {
        private static string GetHexString(this byte[] data)
        {
            var buffer = new StringBuilder();

            foreach (var t in data)
            {
                buffer.Append(t.ToString("X2"));
            }

            return buffer.ToString();
        }

        public static string GetHashString(this string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            using var sha = SHA1.Create();
            var hash = sha.ComputeHash(bytes);
            return hash.GetHexString();
        }
    }
}
