using System.Net.Http;

namespace Sentry.Tests.Helpers;

public abstract class MockableHttpMessageHandler : HttpMessageHandler
{
    public abstract Task<HttpResponseMessage> VerifiableSendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken);

    protected sealed override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => VerifiableSendAsync(request, cancellationToken);
}
