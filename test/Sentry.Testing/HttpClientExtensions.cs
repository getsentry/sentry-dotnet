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
    }
}
