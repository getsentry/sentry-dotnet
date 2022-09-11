using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Sentry.Http;

namespace Sentry.Benchmarks;

internal class FakeHttpClientFactory : ISentryHttpClientFactory
{
    public HttpClient Create(SentryOptions options) => new(new FakeMessageHandler());
}

internal class FakeMessageHandler : DelegatingHandler
{
    private readonly Task<HttpResponseMessage> _result;

    public FakeMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK)
        => _result = Task.FromResult<HttpResponseMessage>(new StatusCodeResponse(statusCode));

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => _result;
}

internal class StatusCodeResponse : HttpResponseMessage
{
    public StatusCodeResponse(HttpStatusCode statusCode)
        : base(statusCode)
    {
        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            Headers.RetryAfter = new(TimeSpan.FromHours(24));
        }
    }
}
