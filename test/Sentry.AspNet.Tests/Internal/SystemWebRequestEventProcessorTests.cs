using Sentry.AspNet.Internal;

namespace Sentry.AspNet.Tests.Internal;

public class SystemWebRequestEventProcessorTests :
    HttpContextTest
{
    private class Fixture
    {
        public IRequestPayloadExtractor RequestPayloadExtractor { get; set; } = Substitute.For<IRequestPayloadExtractor>();
        public SentryOptions SentryOptions { get; set; } = new();
        public object MockBody { get; set; } = new();

        public Fixture()
        {
            _ = RequestPayloadExtractor.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(MockBody);
        }

        public SystemWebRequestEventProcessor GetSut()
        {
            return new SystemWebRequestEventProcessor(RequestPayloadExtractor, SentryOptions);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Ctor_NullEvent_ThrowsArgumentNullException()
    {
        _fixture.RequestPayloadExtractor = null;
        _ = Assert.Throws<ArgumentNullException>(() => _fixture.GetSut());
    }

    [Fact]
    public void Process_NullEvent_ReturnsNull() => Assert.Null(_fixture.GetSut().Process(null));

    [Fact]
    public void Process_DefaultFixture_ReadsMockBody()
    {
        var expected = new SentryEvent();

        var sut = _fixture.GetSut();

        var actual = sut.Process(expected);
        Assert.Same(expected, actual);
        Assert.Same(_fixture.MockBody, expected.Request.Data);
    }

    [Fact]
    public void Process_QueryStringWithSensitiveParameter_FiltersValueAndPreservesEncoding()
    {
        const string query = "token=abc123&q=a%20b%26c&page=5";
        Context = new HttpContext(
            new HttpRequest("test", $"http://test/the/path?{query}", query),
            new HttpResponse(TextWriter.Null));
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Equal("token=[Filtered]&q=a%20b%26c&page=5", evt.Request.QueryString);
        Assert.Equal("http://test/the/path", evt.Request.Url);
    }

    [Fact]
    public void Process_NoHttpContext_NoRequestData()
    {
        Context = null;
        var expected = new SentryEvent();

        var sut = _fixture.GetSut();

        var actual = sut.Process(expected);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void Process_NoBodyExtracted_NoRequestData()
    {
        _ = _fixture.RequestPayloadExtractor.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(null);
        var expected = new SentryEvent();

        var sut = _fixture.GetSut();

        var actual = sut.Process(expected);
        Assert.Same(expected, actual);
        Assert.Null(expected.Request.Data);
    }

    [Fact]
    public void Process_PresetUserIP_NotOverwritten()
    {
        const string userIp = "192.0.0.1";
        var evt = new SentryEvent();
        evt.User.IpAddress = userIp;

        Context = HttpContextBuilder.Build();
        // Ensure user is not null
        Context.User = new GenericPrincipal(new GenericIdentity("TestUser"), null);
        _fixture.SentryOptions.SendDefaultPii = true;
        var sut = _fixture.GetSut();

        var processedEvt = sut.Process(evt);
        Assert.Equal(userIp, processedEvt?.User.IpAddress);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Process_SensitiveRequestHeaders_KeepsNamesAndFiltersValues(bool sendDefaultPii)
    {
        Context = HttpContextBuilder.BuildWithHeaders(
            ["Authorization", "Bearer secret"],
            ["X-Api-Key", "secret"],
            ["Set-Cookie", "name=secret"],
            ["X-Request-Id", "abc"]);
        Context.User = new GenericPrincipal(new GenericIdentity("TestUser"), null);
        _fixture.SentryOptions.SendDefaultPii = sendDefaultPii;
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Equal("[Filtered]", evt.Request.Headers["Authorization"]);
        Assert.Equal("[Filtered]", evt.Request.Headers["X-Api-Key"]);
        Assert.Equal("[Filtered]", evt.Request.Headers["Set-Cookie"]);
        Assert.Equal("abc", evt.Request.Headers["X-Request-Id"]);
    }

    [Fact]
    public void Process_SendDefaultPiiTrue_FiltersCookieHeaderValue()
    {
        Context = HttpContextBuilder.BuildWithHeaders(["Cookie", "name=secret"]);
        Context.User = new GenericPrincipal(new GenericIdentity("TestUser"), null);
        _fixture.SentryOptions.SendDefaultPii = true;
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Equal("[Filtered]", evt.Request.Headers["Cookie"]);
    }

    [Fact]
    public void Process_SendDefaultPiiFalse_DoesNotCaptureCookieHeader()
    {
        Context = HttpContextBuilder.BuildWithHeaders(["Cookie", "name=secret"]);
        _fixture.SentryOptions.SendDefaultPii = false;
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.False(evt.Request.Headers.ContainsKey("Cookie"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Process_LowercaseCookieHeader_FiltersValue(bool sendDefaultPii)
    {
        Context = HttpContextBuilder.BuildWithHeaders(["cookie", "name=secret"]);
        Context.User = new GenericPrincipal(new GenericIdentity("TestUser"), null);
        _fixture.SentryOptions.SendDefaultPii = sendDefaultPii;
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Equal("[Filtered]", evt.Request.Headers["cookie"]);
    }
}
