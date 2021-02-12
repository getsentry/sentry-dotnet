using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Sentry
{
    public static class HttpClientExtensions
    {
        public static IEnumerable<HttpMessageHandler> GetMessageHandlers(this HttpClient client)
        {
            var invoker = typeof(HttpMessageInvoker);
            var fieldInfo = invoker.GetField("handler", BindingFlags.NonPublic | BindingFlags.Instance)
                            ?? invoker.GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);

            if (!(fieldInfo.GetValue(client) is HttpMessageHandler handler))
            {
                yield break;
            }

            do
            {
                yield return handler;
                handler = (handler as DelegatingHandler)?.InnerHandler;
            } while (handler != null);
        }

        public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage source)
        {
            var clone = new HttpRequestMessage(source.Method, source.RequestUri) {Version = source.Version};

            // Headers
            foreach (var (key, value) in source.Headers)
            {
                clone.Headers.TryAddWithoutValidation(key, value);
            }

            // Content
            if (source.Content != null)
            {
                var cloneContentStream = new MemoryStream();

                await source.Content.CopyToAsync(cloneContentStream).ConfigureAwait(false);
                cloneContentStream.Position = 0;

                clone.Content = new StreamContent(cloneContentStream);

                // Content headers
                foreach (var (key, value) in source.Content.Headers)
                {
                    clone.Content.Headers.Add(key, value);
                }
            }

            return clone;
        }
    }
}
