using FluentAssertions.Execution;

namespace Sentry.Tests;

public class SentryFailedRequestHandlerTests
{
    private readonly IHub _hub;

    public SentryFailedRequestHandlerTests()
    {
        _hub = Substitute.For<IHub>();
    }

    private SentryFailedRequestHandler GetSut(SentryOptions options)
    {
        return new SentryFailedRequestHandler(_hub, options);
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
            FailedRequestTargets = new List<SubstringOrRegexPattern> { "http://foo/" }
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
        _hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>(), Arg.Any<Scope>());
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
        _hub.CaptureEvent(Arg.Do<SentryEvent>(e => @event = e));
        sut.HandleResponse(response);

        // Assert
        using (new AssertionScope())
        {
            @event.Should().NotBeNull();

            // Ensure the mechanism is set
            @event.Exception.Data[Mechanism.MechanismKey].Should().Be(
                SentryFailedRequestHandler.MechanismType
                );

            // Ensure the request properties are captured
            @event.Request.Method.Should().Be(HttpMethod.Post.ToString());
            @event.Request.Url.Should().Be(absoluteUri);
            @event.Request.QueryString.Should().Be(queryString);

            // Ensure the response context is captured
            @event.Contexts.Should().Contain(x =>
                x.Key == SentryFailedRequestHandler.ResponseKey
                && x.Value is ResponseContext
                );
            var responseContext = @event.Contexts[SentryFailedRequestHandler.ResponseKey] as ResponseContext;
            responseContext?.StatusCode.Should().Be((short)response.StatusCode);
            responseContext?.BodySize.Should().Be(response.Content.Headers.ContentLength);
            responseContext?.Headers.Count.Should().Be(1);
            responseContext?.Headers.Should().ContainKey("myHeader");
            responseContext?.Headers.Should().ContainValue("myValue");
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
        _hub.CaptureEvent(Arg.Do<SentryEvent>(e => @event = e));
        sut.HandleResponse(response);

        // Assert
        using (new AssertionScope())
        {
            @event.Should().NotBeNull();

            // Cookies and headers are not captured
            var responseContext = @event.Contexts[SentryFailedRequestHandler.ResponseKey] as ResponseContext;
            responseContext?.Headers.Should().BeNullOrEmpty();
            responseContext?.Cookies.Should().BeNullOrEmpty();
        }
    }
}
