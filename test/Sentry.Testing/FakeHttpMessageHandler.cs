using System.Net;
using System.Net.Http;

namespace Sentry.Testing;

public class FakeHttpMessageHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _getResponse;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> getResponse) =>
        _getResponse = getResponse;

    public FakeHttpMessageHandler(Func<HttpResponseMessage> getResponse)
        : this(_ => getResponse()) { }

    public FakeHttpMessageHandler() { }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            _getResponse is not null
                ? _getResponse(request)
                : new(HttpStatusCode.OK));
    }
}
