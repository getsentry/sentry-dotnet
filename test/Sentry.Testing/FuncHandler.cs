using System.Net.Http;

namespace Sentry.Testing;

public class FuncHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> SendAsyncFunc { get; set; }

    public bool SendAsyncCalled { get; set; }

    public FuncHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> sendAsyncFunc = null)
        => SendAsyncFunc = sendAsyncFunc;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        SendAsyncCalled = true;
        return Task.FromResult(SendAsyncFunc?.Invoke(request, cancellationToken));
    }
}
