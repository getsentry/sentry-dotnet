using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Internal.Extensions
{
    internal static class HttpClientExtensions
    {
        public static async Task<JsonElement> ReadAsJsonAsync(
            this HttpContent content,
            CancellationToken cancellationToken = default)
        {
            var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#if NET461 || NETSTANDARD2_0
            using (stream)
#else
            await using (stream.ConfigureAwait(false))
#endif
            {
                using var document = await JsonDocument.ParseAsync(stream, default, cancellationToken).ConfigureAwait(false);

                return document.RootElement.Clone();
            }
        }
    }
}
