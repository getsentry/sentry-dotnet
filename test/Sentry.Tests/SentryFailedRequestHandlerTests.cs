using FluentAssertions.Execution;

namespace Sentry.Tests;

public class SentryFailedRequestHandlerTests
{
    [Fact]
    public void CaptureEvent_CaptureFailedRequests_Disabled_NoEvent()
    {
        // Arrange        
        var hub = Substitute.For<IHub>();
        var sut = new SentryFailedRequestHandler(hub, new SentryOptions
        {
            CaptureFailedRequests = false
        });
        var request = new HttpRequestMessage();
        var response = new HttpResponseMessage();

        // Act
        sut.CaptureEvent(request, response);

        // Assert
        hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void CaptureEvent_SuccessfulRequets_NoEvent()
    {
        // Arrange        
        var hub = Substitute.For<IHub>();
        var sut = new SentryFailedRequestHandler(hub, new SentryOptions
        {
            CaptureFailedRequests = true,
            FailedRequestStatusCodes = new List<HttpStatusCodeRange> { (500, 599) }
        });
        var request = new HttpRequestMessage();
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden); // 403 so not in range

        // Act
        sut.CaptureEvent(request, response);

        // Assert
        hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void CaptureEvent_RequestIsDsn_NoEvent()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537",
            FailedRequestStatusCodes = new List<HttpStatusCodeRange> { (500, 599) }
        };
        var hub = Substitute.For<IHub>();
        var sut = new SentryFailedRequestHandler(hub, options);

        var request = new HttpRequestMessage(HttpMethod.Post, options.Dsn); // Don't capture requests to the Dsn
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError); 

        // Act
        sut.CaptureEvent(request, response);

        // Assert
        hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void CaptureEvent_NoFailedRequestTargetMatch_NoEvent()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537",
            FailedRequestStatusCodes = new List<HttpStatusCodeRange> { (500, 599) },
            FailedRequestTargets = new List<SubstringOrRegexPattern> { "http://foo/" }
        };
        var hub = Substitute.For<IHub>();
        var sut = new SentryFailedRequestHandler(hub, options);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://bar/"); // Don't capture requests to the Dsn
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        // Act
        sut.CaptureEvent(request, response);

        // Assert
        hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void CaptureEvent_FailedRequestTargetMatch_CapturesEvent()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537",
            FailedRequestStatusCodes = new List<HttpStatusCodeRange> { (500, 599) },
            FailedRequestTargets = new List<SubstringOrRegexPattern> { "http://foo", "http://bar" }
        };
        var hub = Substitute.For<IHub>();
        var sut = new SentryFailedRequestHandler(hub, options);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://foo/bar"); // This should match
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError); // This is in the range

        // Act
        sut.CaptureEvent(request, response);

        // Assert
        SentryEvent captured = null;
        hub.Received(1).CaptureEvent(Arg.Do<SentryEvent>(@event => captured = @event));

        using (new AssertionScope())
        {
            captured.Request.Method.Should().Be(HttpMethod.Post.ToString());
            captured.Request.Url.Should().Be("http://foo/bar");
        }
    }
}
