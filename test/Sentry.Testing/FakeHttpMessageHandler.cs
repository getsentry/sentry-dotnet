namespace Sentry.Testing;

public class FakeHttpMessageHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _getResponse;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> getResponse) =>
        _getResponse = getResponse;

    public FakeHttpMessageHandler(Func<HttpResponseMessage> getResponse)
        : this(_ => getResponse()) { }

    public FakeHttpMessageHandler() { }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(_getResponse?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK));

#if NET5_0_OR_GREATER
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
        _getResponse?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK);
#endif
}
