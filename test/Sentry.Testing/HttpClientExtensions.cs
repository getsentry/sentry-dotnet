// ReSharper disable once CheckNamespace
namespace Sentry;

public static class HttpClientExtensions
{
    public static IEnumerable<HttpMessageHandler> GetMessageHandlers(this HttpClient client)
    {
        var invoker = typeof(HttpMessageInvoker);
        var fieldInfo = invoker.GetField("handler", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? invoker.GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);

        if (fieldInfo.GetValue(client) is not HttpMessageHandler handler)
        {
            yield break;
        }

        do
        {
            yield return handler;
            handler = (handler as DelegatingHandler)?.InnerHandler;
        } while (handler != null);
    }

    public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage source,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri) { Version = source.Version };

        // Headers
        foreach (var (key, value) in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(key, value);
        }

        // Content
        if (source.Content != null)
        {
            var cloneContentStream = new MemoryStream();

#if NET5_0_OR_GREATER
            await source.Content.CopyToAsync(cloneContentStream, cancellationToken).ConfigureAwait(false);
#else
            await source.Content.CopyToAsync(cloneContentStream).ConfigureAwait(false);
#endif
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

#if NET5_0_OR_GREATER
    public static HttpRequestMessage Clone(this HttpRequestMessage source, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri) { Version = source.Version };

        // Headers
        foreach (var (key, value) in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(key, value);
        }

        // Content
        if (source.Content != null)
        {
            var cloneContentStream = new MemoryStream();

            source.Content.CopyTo(cloneContentStream, default, cancellationToken);
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

    public static HttpResponseMessage Get(this HttpClient client, string requestUri,
        CancellationToken cancellationToken = default)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
        // .NET 11 annotates the synchronous HttpClient.Send as unsupported on iOS/MacCatalyst.
        // Every test using this helper supplies a substitute inner handler, so the request is
        // answered in-process and never reaches the platform transport that has no sync path.
#pragma warning disable CA1416
        return client.Send(message, cancellationToken);
#pragma warning restore CA1416
    }
#endif
}
