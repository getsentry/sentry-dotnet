namespace Sentry.Testing;

public class CallbackHttpClientHandler : HttpClientHandler
{
    private readonly Func<HttpRequestMessage, Task> _asyncMessageCallback;

    public CallbackHttpClientHandler(Func<HttpRequestMessage, Task> asyncMessageCallback) => _asyncMessageCallback = asyncMessageCallback;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await _asyncMessageCallback(request);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}
