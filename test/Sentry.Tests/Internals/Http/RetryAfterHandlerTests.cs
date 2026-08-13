using Sentry.Internal.Http;
using static System.Threading.CancellationToken;

namespace Sentry.Tests.Internals.Http;

public class RetryAfterHandlerTests
{
    private class Fixture
    {
        public static DateTimeOffset TimeReturned { get; set; } = DateTimeOffset.UtcNow;

        public ISystemClock Clock { get; } = new MockClock(TimeReturned);
        public FuncHandler StubHandler { get; } = new();
        public RetryAfterHandler Sut { get; private set; }

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
        Assert.Equal((Fixture.TimeReturned + RetryAfterHandler.DefaultRetryAfterDelay).UtcTicks, _fixture.Sut.RetryAfterUtcTicks);
        Assert.True(_fixture.StubHandler.SendAsyncCalled);
    }

    [Fact]
    public async Task SendAsync_TooManyRequestsWithCategoryRateLimits_RetryAfterNotSet()
    {
        // Per-category limits are applied by the transport, per envelope item. Backing off globally here would
        // stop us sending categories that aren't rate limited at all. See https://github.com/getsentry/sentry-dotnet/issues/3947
        var expected = new HttpResponseMessage(TooManyRequests);
        expected.Headers.Add("X-Sentry-Rate-Limits", "60:transaction;profile;span:organization:transaction_usage_exceeded");
        expected.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
        _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;

        var invoker = _fixture.GetInvoker();
        var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

        Assert.Equal(expected, actual);
        Assert.Equal(0, _fixture.Sut.RetryAfterUtcTicks);
        Assert.True(_fixture.StubHandler.SendAsyncCalled);
    }

    [Fact]
    public async Task SendAsync_TooManyRequestsWithCategoryRateLimits_SecondRequestIsNotThrottled()
    {
        var rateLimited = new HttpResponseMessage(TooManyRequests);
        rateLimited.Headers.Add("X-Sentry-Rate-Limits", "60:transaction;profile;span:organization:transaction_usage_exceeded");
        _fixture.StubHandler.SendAsyncFunc = (_, _) => rateLimited;

        var invoker = _fixture.GetInvoker();

        // First call: rate limited for transactions only
        _ = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);
        Assert.True(_fixture.StubHandler.SendAsyncCalled);

        // Change the response: OK
        var expected = new HttpResponseMessage(HttpStatusCode.OK);
        _fixture.StubHandler.SendAsyncFunc = (_, _) => expected;
        _fixture.StubHandler.SendAsyncCalled = false;

        var actual = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"), None);

        Assert.Equal(expected, actual);
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
        Assert.Equal((Fixture.TimeReturned + delta).UtcTicks, _fixture.Sut.RetryAfterUtcTicks);
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
        var expectedTime = Fixture.TimeReturned.AddTicks((long)(floating * TimeSpan.TicksPerSecond));
        Assert.Equal(expectedTime.UtcTicks, _fixture.Sut.RetryAfterUtcTicks);
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
        var expectedTime = Fixture.TimeReturned.AddTicks((long)(floating * TimeSpan.TicksPerSecond));
        Assert.Equal(expectedTime.UtcTicks, _fixture.Sut.RetryAfterUtcTicks);
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
        Assert.Equal((Fixture.TimeReturned + delta).UtcTicks, _fixture.Sut.RetryAfterUtcTicks);
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
