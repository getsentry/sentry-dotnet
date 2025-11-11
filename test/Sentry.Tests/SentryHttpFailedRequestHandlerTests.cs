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
        _hub.DidNotReceiveWithAnyArgs().CaptureEvent(null!);
    }

    [Theory]
    [InlineData(100)] // Continue
    [InlineData(101)] // Switching Protocols
    [InlineData(199)] // Edge of informational range
    [InlineData(200)] // OK
    [InlineData(201)] // Created
    [InlineData(299)] // Edge of success range
    [InlineData(300)] // Multiple Choices
    [InlineData(301)] // Moved Permanently
    [InlineData(399)] // Edge of redirect range
    [InlineData(400)] // Bad Request
    [InlineData(401)] // Unauthorized
    [InlineData(403)] // Forbidden
    [InlineData(404)] // Not Found
    [InlineData(499)] // Edge of client error range
    [InlineData(600)] // Beyond standard range
    public void HandleResponse_EnabledButNotInRange_DontCapture(int statusCode)
    {
        // Arrange
        var sut = GetSut(new SentryOptions
        {
            CaptureFailedRequests = true
            // default FailedRequestStatusCodes = (500,599)
        });

        var response = new HttpResponseMessage((HttpStatusCode)statusCode);
        response.RequestMessage = new HttpRequestMessage();

        // Act
        sut?.HandleResponse(response);

        // Assert
        _hub?.DidNotReceiveWithAnyArgs().CaptureEvent(null!);
    }

    [Theory]
    [InlineData(400)] // Bad Request - in range
    [InlineData(401)] // Unauthorized - in range
    [InlineData(404)] // Not Found - in range
    [InlineData(499)] // Edge of range
    public void HandleResponse_CustomRange_InRange_DoCapture(int statusCode)
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            FailedRequestStatusCodes = new List<HttpStatusCodeRange> { (400, 499) }
        };
        var sut = GetSut(options);

        var response = new HttpResponseMessage((HttpStatusCode)statusCode);
        response.RequestMessage = new HttpRequestMessage();

        // Act
        sut.HandleResponse(response);

        // Assert
        _hub.ReceivedWithAnyArgs(1).CaptureEvent(null!);
    }

    [Theory]
    [InlineData(200)] // OK - below range
    [InlineData(399)] // Edge below range
    [InlineData(500)] // Internal Server Error - above range
    [InlineData(503)] // Service Unavailable - above range
    public void HandleResponse_CustomRange_OutOfRange_DontCapture(int statusCode)
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            FailedRequestStatusCodes = new List<HttpStatusCodeRange> { (400, 499) }
        };
        var sut = GetSut(options);

        var response = new HttpResponseMessage((HttpStatusCode)statusCode);
        response.RequestMessage = new HttpRequestMessage();

        // Act
        sut.HandleResponse(response);

        // Assert
        _hub.DidNotReceiveWithAnyArgs().CaptureEvent(null!);
    }

    [Theory]
    [InlineData(200)] // OK
    [InlineData(201)] // Created
    [InlineData(204)] // No Content
    [InlineData(299)] // Edge of success range
    public void HandleResponse_RangeIncludesSuccess_SuccessNotCaptured(int statusCode)
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            // Misconfigured to include success codes - they should still not be captured
            FailedRequestStatusCodes = new List<HttpStatusCodeRange> { (200, 599) }
        };
        var sut = GetSut(options);

        var response = new HttpResponseMessage((HttpStatusCode)statusCode);
        response.RequestMessage = new HttpRequestMessage();

        // Act
        sut.HandleResponse(response);

        // Assert
        // Success codes should never be captured, even if in configured range
        _hub.DidNotReceiveWithAnyArgs().CaptureEvent(null!);
    }

    [Fact]
    public void HandleResponse_RequestsToSentryDsn_DontCapture()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            Dsn = ValidDsn
        };
        var sut = GetSut(options);

        var response = InternalServerErrorResponse();
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Post, options.Dsn);

        // Act
        sut.HandleResponse(response);

        // Assert
        _hub.DidNotReceiveWithAnyArgs().CaptureEvent(null!);
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
        _hub.DidNotReceiveWithAnyArgs().CaptureEvent(null!);
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
        _hub.ReceivedWithAnyArgs(1).CaptureEvent(null!);
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

    [Fact]
    public void HandleResponse_ExceptionHasStackTrace()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true
        };
        var sut = GetSut(options);

        var response = InternalServerErrorResponse();
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com/api/test");

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
            @event.Exception.Should().NotBeNull();
            @event.Exception!.StackTrace.Should().NotBeNullOrWhiteSpace();
        }
    }
}
