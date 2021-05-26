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
#if !NET461 && !NETSTANDARD2_0
            await
#endif
            using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var jsonDocument = await JsonDocument.ParseAsync(stream, default, cancellationToken).ConfigureAwait(false);

            return jsonDocument.RootElement.Clone();
        }
    }
}
