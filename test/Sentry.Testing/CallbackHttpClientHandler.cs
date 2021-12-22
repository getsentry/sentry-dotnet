using System.Net;
using System.Net.Http;

namespace Sentry.Testing;

public class CallbackHttpClientHandler : HttpClientHandler
{
    private readonly Action<HttpRequestMessage> _messageCallback;

    public CallbackHttpClientHandler(Action<HttpRequestMessage> messageCallback) => _messageCallback = messageCallback;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _messageCallback(request);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
