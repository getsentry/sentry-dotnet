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

    [Fact]
    public void Process_SendDefaultPiiTrue_FiltersCookies()
    {
        Context = HttpContextBuilder.BuildWithCookies(new[]
        {
            new HttpCookie("user_session", "abc"),
            new HttpCookie("theme", "dark-mode")
        });
        Context.User = new GenericPrincipal(new GenericIdentity("TestUser"), null);
        _fixture.SentryOptions.SendDefaultPii = true;

        var processedEvt = _fixture.GetSut().Process(new SentryEvent());

        processedEvt?.Request.Cookies.Should().Be("user_session=[Filtered]; theme=dark-mode");
    }

    [Fact]
    public void Process_SendDefaultPiiTrue_KeepsEachCookieWithDuplicateName()
    {
        Context = HttpContextBuilder.BuildWithCookies(new[]
        {
            new HttpCookie("theme", "dark-mode"),
            new HttpCookie("theme", "light-mode")
        });
        Context.User = new GenericPrincipal(new GenericIdentity("TestUser"), null);
        _fixture.SentryOptions.SendDefaultPii = true;

        var processedEvt = _fixture.GetSut().Process(new SentryEvent());

        processedEvt?.Request.Cookies.Should().Be("theme=dark-mode; theme=light-mode");
    }

    [Fact]
    public void Process_SendDefaultPiiFalse_DoesNotSetCookies()
    {
        Context = HttpContextBuilder.BuildWithCookies(new[] { new HttpCookie("theme", "dark-mode") });
        _fixture.SentryOptions.SendDefaultPii = false;

        var processedEvt = _fixture.GetSut().Process(new SentryEvent());

        processedEvt?.Request.Cookies.Should().BeNull();
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

    [Fact]
    public void Process_DataCollectionUserInfoTrue_SetsUserWithoutSendDefaultPii()
    {
        Context.User = CreateClaimsPrincipal();
        _fixture.SentryOptions.SendDefaultPii = false;
        _fixture.SentryOptions.ConfigureDataCollection(_ => { });
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Equal("TestUser", evt.User.Username);
        Assert.Equal("user-id", evt.User.Id);
        Assert.Equal("True", evt.User.Other["IsAuthenticated"]);
    }

    [Fact]
    public void Process_DataCollectionUserInfoFalse_DoesNotSetUser()
    {
        Context = HttpContextBuilder.BuildWithHeaders();
        Context.User = CreateClaimsPrincipal();
        _fixture.SentryOptions.SendDefaultPii = true;
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection => dataCollection.UserInfo = false);
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Null(evt.User.Username);
        Assert.Null(evt.User.Id);
        Assert.Null(evt.User.IpAddress);
        Assert.Empty(evt.User.Other);
    }

    [Fact]
    public void Process_DataCollectionUserInfoTrueWithoutUser_ReturnsEvent()
    {
        Context.User = null;
        _fixture.SentryOptions.ConfigureDataCollection(_ => { });
        var evt = new SentryEvent();

        var processedEvt = _fixture.GetSut().Process(evt);

        Assert.Same(evt, processedEvt);
        Assert.Null(evt.User.Username);
    }

    [Fact]
    public void Process_DataCollectionCookiesOff_DoesNotSetCookies()
    {
        Context = HttpContextBuilder.BuildWithCookies(new[] { new HttpCookie("theme", "dark-mode") });
        _fixture.SentryOptions.SendDefaultPii = true;
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection => dataCollection.Cookies = KeyValueCollectionBehavior.Off);

        var processedEvt = _fixture.GetSut().Process(new SentryEvent());

        processedEvt?.Request.Cookies.Should().BeNull();
    }

    [Fact]
    public void Process_DataCollectionCookiesOff_DoesNotCaptureCookieHeader()
    {
        Context = HttpContextBuilder.BuildWithHeaders(["Cookie", "name=secret"], ["X-Request-Id", "abc"]);
        _fixture.SentryOptions.SendDefaultPii = true;
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection => dataCollection.Cookies = KeyValueCollectionBehavior.Off);
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.False(evt.Request.Headers.ContainsKey("Cookie"));
        Assert.Equal("abc", evt.Request.Headers["X-Request-Id"]);
    }

    [Fact]
    public void Process_DataCollectionCookiesAllowList_FiltersCookiesNotAllowed()
    {
        Context = HttpContextBuilder.BuildWithCookies(new[]
        {
            new HttpCookie("theme", "dark-mode"),
            new HttpCookie("lang", "en")
        });
        _fixture.SentryOptions.SendDefaultPii = false;
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection => dataCollection.Cookies = KeyValueCollectionBehavior.AllowList("theme"));

        var processedEvt = _fixture.GetSut().Process(new SentryEvent());

        processedEvt?.Request.Cookies.Should().Be("theme=dark-mode; lang=[Filtered]");
    }

    [Fact]
    public void Process_DataCollectionCookiesDenyList_FiltersCookieHeaderValueWithoutSendDefaultPii()
    {
        Context = HttpContextBuilder.BuildWithHeaders(["Cookie", "name=secret"]);
        _fixture.SentryOptions.SendDefaultPii = false;
        _fixture.SentryOptions.ConfigureDataCollection(_ => { });
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Equal("[Filtered]", evt.Request.Headers["Cookie"]);
    }

    [Fact]
    public void Process_DataCollectionRequestHeadersOff_DoesNotCaptureHeaders()
    {
        Context = HttpContextBuilder.BuildWithHeaders(["X-Request-Id", "abc"]);
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection =>
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.Off);
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Empty(evt.Request.Headers);
    }

    [Fact]
    public void Process_DataCollectionRequestHeadersOff_StillCapturesCookies()
    {
        Context = HttpContextBuilder.BuildWithCookies(new[] { new HttpCookie("theme", "dark-mode") });
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection =>
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.Off);
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Empty(evt.Request.Headers);
        Assert.Equal("theme=dark-mode", evt.Request.Cookies);
    }

    [Fact]
    public void Process_DataCollectionRequestHeadersAllowList_FiltersHeadersNotAllowed()
    {
        Context = HttpContextBuilder.BuildWithHeaders(["X-Request-Id", "abc"], ["X-Tenant", "acme"]);
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection =>
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.AllowList("Request-Id"));
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Equal("abc", evt.Request.Headers["X-Request-Id"]);
        Assert.Equal("[Filtered]", evt.Request.Headers["X-Tenant"]);
    }

    [Fact]
    public void Process_DataCollectionUrlQueryParamsOff_DoesNotSetQueryString()
    {
        Context = BuildWithQuery("q=search&page=5");
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection =>
            dataCollection.UrlQueryParams = KeyValueCollectionBehavior.Off);
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Null(evt.Request.QueryString);
    }

    [Fact]
    public void Process_DataCollectionUrlQueryParamsAllowList_FiltersParametersNotAllowed()
    {
        Context = BuildWithQuery("q=search&page=5");
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection =>
            dataCollection.UrlQueryParams = KeyValueCollectionBehavior.AllowList("q"));
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Equal("q=search&page=[Filtered]", evt.Request.QueryString);
    }

    [Fact]
    public void Process_DataCollectionHttpBodiesWithoutIncomingRequest_DoesNotReadBody()
    {
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection =>
            dataCollection.HttpBodies = HttpBodyTypes.All & ~HttpBodyTypes.IncomingRequest);
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Null(evt.Request.Data);
        _ = _fixture.RequestPayloadExtractor.DidNotReceive().ExtractPayload(Arg.Any<IHttpRequest>());
    }

    [Fact]
    public void Process_DataCollectionHttpBodiesWithIncomingRequest_ReadsBody()
    {
        _fixture.SentryOptions.ConfigureDataCollection(dataCollection =>
            dataCollection.HttpBodies = HttpBodyTypes.IncomingRequest);
        var evt = new SentryEvent();

        _fixture.GetSut().Process(evt);

        Assert.Same(_fixture.MockBody, evt.Request.Data);
    }

    private static ClaimsPrincipal CreateClaimsPrincipal() =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "TestUser"), new Claim(ClaimTypes.NameIdentifier, "user-id")],
            authenticationType: "test"));

    private static HttpContext BuildWithQuery(string query) =>
        new(new HttpRequest("test", $"http://test/the/path?{query}", query), new HttpResponse(TextWriter.Null));
}
