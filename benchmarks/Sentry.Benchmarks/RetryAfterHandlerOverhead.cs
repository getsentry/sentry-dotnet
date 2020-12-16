using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Sentry.Internal.Http;
using static System.Threading.CancellationToken;

namespace Sentry.Benchmarks
{
    public class RetryAfterHandlerOverhead
    {
        private HttpMessageInvoker _invoker;
        private readonly HttpRequestMessage _request = new(HttpMethod.Get, "/");

        [Params(1, 10, 100)] public int RequestCount;

        [GlobalSetup(Target = nameof(With_RetryAfterHandler_OkResponse))]
        public void Setup_With_RetryAfterHandler_OkResponse()
            => _invoker = new HttpMessageInvoker(new RetryAfterHandler(new FakeMessageHandler()));

        [GlobalSetup(Target = nameof(With_RetryAfterHandler_429Response))]
        public void Setup_With_RetryAfterHandler_429Response()
            => _invoker = new HttpMessageInvoker(new RetryAfterHandler(new FakeMessageHandler(HttpStatusCode.TooManyRequests)));

        [GlobalSetup(Target = nameof(Without_RetryAfterHandler))]
        public void Setup_Without_RetryAfterHandler() => _invoker = new HttpMessageInvoker(new FakeMessageHandler());

        [Benchmark(Baseline = true, Description = "Without RetryAfterHandler")]
        public async Task Without_RetryAfterHandler()
        {
            for (var i = 0; i < RequestCount; i++)
            {
                _ = await _invoker.SendAsync(_request, None);
            }
        }

        [Benchmark(Description = "With RetryAfterHandler OK response")]
        public async Task With_RetryAfterHandler_OkResponse()
        {
            for (var i = 0; i < RequestCount; i++)
            {
                _ = await _invoker.SendAsync(_request, None);
            }
        }

        [Benchmark(Description = "With RetryAfterHandler 429 response")]
        public async Task With_RetryAfterHandler_429Response()
        {
            for (var i = 0; i < RequestCount; i++)
            {
                _ = await _invoker.SendAsync(_request, None);
            }
        }
    }
}
