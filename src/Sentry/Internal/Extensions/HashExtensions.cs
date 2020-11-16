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

        public static string GetHashString(this string str, HashAlgorithm algo)
        {
            using (algo)
            {
                var hashData = algo.ComputeHash(Encoding.UTF8.GetBytes(str));
                return hashData.GetHexString();
            }
        }

        public static string GetHashString(this string str) =>
            str.GetHashString(SHA1.Create());
    }
}
