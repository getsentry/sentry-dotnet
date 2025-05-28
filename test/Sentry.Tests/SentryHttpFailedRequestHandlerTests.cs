namespace Sentry.Tests;

public class SentryHttpFailedRequestHandlerTests
{
    private readonly IHub _hub;

    public SentryHttpFailedRequestHandlerTests()
    {
        _hub = Substitute.For<IHub>();
    }

    private SentryHttpFailedRequestHandler GetSut(SentryOptions options)
    {
        return new SentryHttpFailedRequestHandler(_hub, options);
    }

    private static HttpResponseMessage ForbiddenResponse()
        => new(HttpStatusCode.Forbidden);

    private static HttpResponseMessage InternalServerErrorResponse()
        => new(HttpStatusCode.InternalServerError);

    [Fact]
    public void HandleResponse_Disabled_DontCapture()
    {
        // Arrange
        var sut = GetSut(new SentryOptions
        {
            CaptureFailedRequests = false
        });

        var response = InternalServerErrorResponse();
        response.RequestMessage = new HttpRequestMessage();

        // Act
        sut.HandleResponse(response);

        // Assert
        _hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void HandleResponse_EnabledButNotInRange_DontCapture()
    {
        // Arrange
        var sut = GetSut(new SentryOptions
        {
            CaptureFailedRequests = true
        });

        var response = ForbiddenResponse(); // 403 is not in default range (500-599)
        response.RequestMessage = new HttpRequestMessage();

        // Act
        sut?.HandleResponse(response);

        // Assert
        _hub?.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void HandleResponse_RequestsToSentryDsn_DontCapture()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537"
        };
        var sut = GetSut(options);

        var response = InternalServerErrorResponse();
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Post, options.Dsn);

        // Act
        sut.HandleResponse(response);

        // Assert
        _hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void HandleResponse_NoMatchingTarget_DontCapture()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            FailedRequestTargets = new List<StringOrRegex> { "http://foo/" }
        };
        var sut = GetSut(options);

        var response = InternalServerErrorResponse();
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://bar/");

        // Act
        sut.HandleResponse(response);

        // Assert
        _hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void HandleResponse_Capture_FailedRequest()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true
        };
        var sut = GetSut(options);

        var response = InternalServerErrorResponse();
        response.RequestMessage = new HttpRequestMessage();

        // Act
        sut.HandleResponse(response);

        // Assert
        _hub.Received(1).CaptureEvent(
            Arg.Any<SentryEvent>(),
            Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void HandleResponse_Capture_FailedRequest_No_Pii()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true
        };
        var sut = GetSut(options);

        var response = InternalServerErrorResponse();
        var requestUri = new Uri("http://admin:1234@localhost/test/path?query=string#fragment");
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

        // Act
        SentryEvent @event = null;
        ((ISentryClient)_hub).CaptureEvent(Arg.Do<SentryEvent>(e => @event = e), hint: Arg.Any<SentryHint>());
        sut.HandleResponse(response);

        // Assert
        @event.Request.Url.Should().Be("http://localhost/test/path?query=string"); // No admin:1234
    }

    [Fact]
    public void HandleResponse_Capture_RequestAndResponse()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            SendDefaultPii = true
        };

        var sut = GetSut(options);

        var url = "http://foo/bar/hello";
        var queryString = "?myQuery=myValue";
        var fragment = "myFragment";
        var absoluteUri = $"{url}{queryString}#{fragment}";
        var response = InternalServerErrorResponse(); // This is in the range
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Post, absoluteUri);
        response.Headers.Add("myHeader", "myValue");
        response.Content = new StringContent("Something broke!", Encoding.UTF8, "text/plain");

        // Act
        SentryEvent @event = null;
        ((ISentryClient)_hub).CaptureEvent(
            Arg.Do<SentryEvent>(e => @event = e),
            hint: Arg.Any<SentryHint>()
            );
        sut.HandleResponse(response);

        // Assert
        using (new AssertionScope())
        {
            @event.Should().NotBeNull();

            // Ensure the mechanism is set
            @event.Exception?.Data[Mechanism.MechanismKey].Should().Be(SentryHttpFailedRequestHandler.MechanismType);

            // Ensure the request properties are captured
            @event.Request.Method.Should().Be(HttpMethod.Post.ToString());
            @event.Request.Url.Should().Be(absoluteUri);
            @event.Request.QueryString.Should().Be(queryString);

            // Ensure the response context is captured
            @event.Contexts.Should().Contain(x => x.Key == Response.Type && x.Value is Response);

            var responseContext = @event.Contexts[Response.Type] as Response;
            responseContext?.StatusCode.Should().Be((short)response.StatusCode);
            responseContext?.BodySize.Should().Be(response.Content.Headers.ContentLength);

            @event.Contexts.Response.Headers.Should().ContainKey("myHeader");
            @event.Contexts.Response.Headers.Should().ContainValue("myValue");
        }
    }

    [Fact]
    public void HandleResponse_Capture_Default_SkipCookiesAndHeaders()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            SendDefaultPii = false
        };
        var sut = GetSut(options);

        var response = InternalServerErrorResponse(); // This is in the range
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://foo/bar");
        response.Headers.Add("myHeader", "myValue");
        response.Content = new StringContent("Something broke!", Encoding.UTF8, "text/plain");
        response.Headers.Add("Cookie", "myCookie=myValue");

        // Act
        SentryEvent @event = null;
        ((ISentryClient)_hub).CaptureEvent(
            Arg.Do<SentryEvent>(e => @event = e),
            hint: Arg.Any<SentryHint>()
            );
        sut.HandleResponse(response);

        // Assert
        using (new AssertionScope())
        {
            @event.Should().NotBeNull();

            // Cookies and headers are not captured
            @event.Contexts.Response.Headers.Should().BeNullOrEmpty();
            @event.Contexts.Response.Cookies.Should().BeNullOrEmpty();
        }
    }

    [Fact]
    public void HandleResponse_Hint_Response()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true
        };
        var sut = GetSut(options);

        var response = InternalServerErrorResponse(); // This is in the range
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://foo/bar");

        // Act
        SentryHint hint = null;
        ((ISentryClient)_hub).CaptureEvent(
            Arg.Any<SentryEvent>(),
            hint: Arg.Do<SentryHint>(h => hint = h)
            );
        sut.HandleResponse(response);

        // Assert
        using (new AssertionScope())
        {
            hint.Should().NotBeNull();

            // Response should be captured
            hint.Items[HintTypes.HttpResponseMessage].Should().Be(response);
        }
    }
}
