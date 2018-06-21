using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Tests.Helpers
{
    public abstract class MockableHttpMessageHandler : HttpMessageHandler
    {
        public abstract Task<HttpResponseMessage> VerifyableSendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken);

        protected sealed override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => VerifyableSendAsync(request, cancellationToken);
    }
}
