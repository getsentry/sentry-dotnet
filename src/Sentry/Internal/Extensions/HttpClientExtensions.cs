using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Sentry.Internal.Extensions
{
    internal static class HttpClientExtensions
    {
        public static async Task<JToken> ReadAsJsonAsync(this HttpContent content)
        {
            var raw = await content.ReadAsStringAsync().ConfigureAwait(false);
            return JToken.Parse(raw);
        }
    }
}
