using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NSubstitute;
using Sentry.Infrastructure;
using Sentry.Internal.Http;
using Sentry.Testing;
using Xunit;
using static System.Threading.CancellationToken;

namespace Sentry.Tests.Internals.Http
{
    public class RetryAfterHandlerTests
    {
        private class Fixture
        {
            public DateTimeOffset TimeReturned { get; set; } = DateTimeOffset.UtcNow;

            public ISystemClock Clock { get; } = Substitute.For<ISystemClock>();
            public FuncHandler StubHandler { get; } = new();
            public RetryAfterHandler Sut { get; private set; }

            public Fixture() => Clock.GetUtcNow().Returns(TimeReturned);

            public HttpMessageInvoker GetInvoker()
            {
                Sut = new RetryAfterHandler(StubHandler, Clock);
                return new HttpMessageInvoker(Sut);
            }
        }

        private const HttpStatusCode TooManyRequests = (HttpStatusCode)429;
        private readonly Fixture _fixture = new();

        [Fact]
        public async Task SendAsync_BadRequest_NoRetryAfterSet()
        {
            var expected = new HttpResponseMessage(HttpStatusCode.BadRequest);
            _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;

            var invoker = _fixture.GetInvoker();
            var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

            Assert.Equal(expected, actual);
            Assert.Equal(0, _fixture.Sut.RetryAfterUtcTicks);
            Assert.True(_fixture.StubHandler.SendAsyncCalled);
        }

        [Fact]
        public async Task SendAsync_TooManyRequestsWithoutRetryAfterHeader_RetryAfterNotSet()
        {
            var expected = new HttpResponseMessage(TooManyRequests);
            _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;

            var invoker = _fixture.GetInvoker();
            var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

            Assert.Equal(expected, actual);
            Assert.Equal(0, _fixture.Sut.RetryAfterUtcTicks);
            Assert.True(_fixture.StubHandler.SendAsyncCalled);
        }

        [Fact]
        public async Task SendAsync_TooManyRequestsWithRetryAfterHeaderDate_RetryAfterSet()
        {
            var expected = new HttpResponseMessage(TooManyRequests);
            var date = DateTimeOffset.MaxValue;
            expected.Headers.RetryAfter = new RetryConditionHeaderValue(date);
            _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;

            var invoker = _fixture.GetInvoker();
            var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

            Assert.Equal(expected, actual);
            Assert.Equal(date.Ticks, _fixture.Sut.RetryAfterUtcTicks);
            Assert.True(_fixture.StubHandler.SendAsyncCalled);
        }

        [Fact]
        public async Task SendAsync_TooManyRequestsWithRetryAfterHeaderDelta_RetryAfterSet()
        {
            var expected = new HttpResponseMessage(TooManyRequests);
            var delta = TimeSpan.FromSeconds(300);
            expected.Headers.RetryAfter = new RetryConditionHeaderValue(delta);

            _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;

            var invoker = _fixture.GetInvoker();
            var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

            Assert.Equal(expected, actual);
            Assert.Equal((_fixture.TimeReturned + delta).UtcTicks, _fixture.Sut.RetryAfterUtcTicks);
            Assert.True(_fixture.StubHandler.SendAsyncCalled);
        }

        [Fact]
        public async Task SendAsync_TooManyRequestsWithRetryAfterHeaderFloat_RetryAfterSet()
        {
            var expected = new HttpResponseMessage(TooManyRequests);
            const double floating = 292.052427053D; // Just under 5 minutes, taken from a Sentry response
            _ = expected.Headers.TryAddWithoutValidation("Retry-After", new[] { floating.ToString(CultureInfo.InvariantCulture) });

            _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;

            var invoker = _fixture.GetInvoker();
            var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

            Assert.Equal(expected, actual);
            Assert.Equal((_fixture.TimeReturned + TimeSpan.FromSeconds(floating)).UtcTicks, _fixture.Sut.RetryAfterUtcTicks);
            Assert.True(_fixture.StubHandler.SendAsyncCalled);
        }

        [Fact]
        public async Task SendAsync_TooManyRequestsWithRetryAfterHeaderFloat_SecondRequestIsThrottled()
        {
            var expected = new HttpResponseMessage(TooManyRequests);
            const double floating = 4138.97064495D; // Taken from a Sentry response
            _ = expected.Headers.TryAddWithoutValidation("Retry-After", new[] { floating.ToString(CultureInfo.InvariantCulture) });

            _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;

            var invoker = _fixture.GetInvoker();

            // First call
            _ = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);
            Assert.True(_fixture.StubHandler.SendAsyncCalled);

            _fixture.StubHandler.SendAsyncCalled = false; // reset
            var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

            Assert.Equal(TooManyRequests, actual.StatusCode);
            Assert.Equal((_fixture.TimeReturned + TimeSpan.FromSeconds(floating)).UtcTicks, _fixture.Sut.RetryAfterUtcTicks);
            Assert.False(_fixture.StubHandler.SendAsyncCalled);
        }

        [Fact]
        public async Task SendAsync_TooManyRequestsWithRetryAfterHeaderDelta_SecondRequestIsThrottled()
        {
            var expected = new HttpResponseMessage(TooManyRequests);
            var delta = TimeSpan.FromSeconds(300);
            expected.Headers.RetryAfter = new RetryConditionHeaderValue(delta);

            _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;

            var invoker = _fixture.GetInvoker();

            // First call
            _ = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);
            Assert.True(_fixture.StubHandler.SendAsyncCalled);

            _fixture.StubHandler.SendAsyncCalled = false; // reset
            var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

            Assert.Equal(TooManyRequests, actual.StatusCode);
            Assert.Equal((_fixture.TimeReturned + delta).UtcTicks, _fixture.Sut.RetryAfterUtcTicks);
            Assert.False(_fixture.StubHandler.SendAsyncCalled);
        }

        [Fact]
        public async Task SendAsync_TooManyRequestsWithRetryAfterHeaderDate_SecondRequestIsThrottled()
        {
            var response = new HttpResponseMessage(TooManyRequests);
            var date = DateTimeOffset.MaxValue;
            response.Headers.RetryAfter = new RetryConditionHeaderValue(date);

            _fixture.StubHandler.SendAsyncFunc = (_, _) => response;

            var invoker = _fixture.GetInvoker();

            // First call
            _ = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);
            Assert.True(_fixture.StubHandler.SendAsyncCalled);

            _fixture.StubHandler.SendAsyncCalled = false; // reset
            var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

            Assert.Equal(TooManyRequests, actual.StatusCode);
            Assert.Equal(date.Ticks, _fixture.Sut.RetryAfterUtcTicks);
            Assert.False(_fixture.StubHandler.SendAsyncCalled);
        }

        [Fact]
        public async Task SendAsync_TooManyRequestsWithRetryAfterHeaderInThePast_SecondRequestIsNotThrottled()
        {
            var response = new HttpResponseMessage(TooManyRequests);
            var date = DateTimeOffset.Now - TimeSpan.FromDays(1);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(date);

            _fixture.StubHandler.SendAsyncFunc = (_, _) => response;

            var invoker = _fixture.GetInvoker();

            // First call: Too Many Requests, RetryAfterUtcTicks
            _ = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);
            Assert.True(_fixture.StubHandler.SendAsyncCalled);
            Assert.Equal(date.UtcTicks, _fixture.Sut.RetryAfterUtcTicks);

            // Change the response: OK
            var expected = new HttpResponseMessage(HttpStatusCode.OK);
            _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;
            _fixture.StubHandler.SendAsyncCalled = false;

            var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

            Assert.Equal(expected, actual);
            Assert.Equal(0, _fixture.Sut.RetryAfterUtcTicks);
            Assert.True(_fixture.StubHandler.SendAsyncCalled);
        }

        [Fact]
        public async Task SendAsync_TooManyRequestsWithRetryAfterHeader_ResponseIsNotReused()
        {
            var expected = new HttpResponseMessage(TooManyRequests);
            var date = DateTimeOffset.MaxValue;
            expected.Headers.RetryAfter = new RetryConditionHeaderValue(date);
            _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;

            var invoker = _fixture.GetInvoker();

            using var first = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);
            using var second = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);
            using var third = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

            Assert.NotSame(first, second);
            Assert.NotSame(second, third);

            // On older frameworks the default content is null
            if (first.Content is not null)
            {
                Assert.NotSame(first.Content, second.Content);
            }

            // On older frameworks the default content is null
            if (second.Content is not null)
            {
                Assert.NotSame(second.Content, third.Content);
            }
        }

        [Fact]
        public void Ctor_NullDateTimeOffsetFunc_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new RetryAfterHandler(Substitute.For<HttpMessageHandler>(), null!));

            Assert.Equal("clock", ex.ParamName);
        }
    }
}
