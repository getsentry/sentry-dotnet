using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Sentry
{
    public static class HttpClientExtensions
    {
        public static IEnumerable<HttpMessageHandler> GetMessageHandlers(this HttpClient client)
        {
            if (!(typeof(HttpMessageInvoker)
                .GetField("_handler",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(client)
                is HttpMessageHandler handler))
            {
                yield break;
            }

            do
            {
                yield return handler;
                handler = (handler as DelegatingHandler)?.InnerHandler;
            } while (handler != null);
        }
    }
}
