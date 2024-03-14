namespace Sentry.Tests;

public class SentryGraphQlHttpFailedRequestHandlerTests
{
    private const string ValidQuery = "query getAllNotes { notes { id } }";
    private static HttpResponseMessage ForbiddenResponse()
        => new(HttpStatusCode.Forbidden);

    private static HttpResponseMessage InternalServerErrorResponse()
        => new(HttpStatusCode.InternalServerError);

    private HttpResponseMessage PreconditionFailedResponse()
        => new(HttpStatusCode.PreconditionFailed)
        {
            Content = SentryGraphQlTestHelpers.ErrorContent("Bad query", "BAD_OP")
        };

    [Fact]
    public void HandleResponse_Disabled_DontCapture()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var options = new SentryOptions
        {
            CaptureFailedRequests = false
        };
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, options);

        var response = InternalServerErrorResponse();
        response.RequestMessage = new HttpRequestMessage();

        // Act
        sut.HandleResponse(response);

        // Assert
        hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void HandleResponse_RequestsToSentryDsn_DontCapture()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537"
        };
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, options);

        var response = InternalServerErrorResponse();
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Post, options.Dsn);

        // Act
        sut.HandleResponse(response);

        // Assert
        hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void HandleResponse_NoMatchingTarget_DontCapture()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            FailedRequestTargets = new List<SubstringOrRegexPattern> { "http://foo/" }
        };
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, options);

        var response = InternalServerErrorResponse();
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://bar/");

        // Act
        sut.HandleResponse(response);

        // Assert
        hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
    }

    [Fact]
    public void HandleResponse_NoError_BaseCapturesFailedRequests()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var options = new SentryOptions
        {
            CaptureFailedRequests = true
        };
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, options);

        var response = InternalServerErrorResponse();
        response.RequestMessage = new HttpRequestMessage();

        // Act
        sut.HandleResponse(response);

        // Assert
        hub.Received(1).CaptureEvent(
            Arg.Any<SentryEvent>(),
            Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void HandleResponse_Error_Capture()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var options = new SentryOptions
        {
            CaptureFailedRequests = true
        };
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, options);

        var response = PreconditionFailedResponse();
        response.RequestMessage = new HttpRequestMessage();

        // Act
        sut.HandleResponse(response);

        // Assert
        hub.Received(1).CaptureEvent(
            Arg.Any<SentryEvent>(),
            Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void HandleResponse_Error_DontSendPii()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var options = new SentryOptions
        {
            CaptureFailedRequests = true
        };
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, options);

        var response = PreconditionFailedResponse();
        var uri = new Uri("http://admin:1234@localhost/test/path?query=string#fragment");
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

        // Act
        SentryEvent @event = null;
        hub.CaptureEvent(Arg.Do<SentryEvent>(e => @event = e), hint: Arg.Any<SentryHint>());
        sut.HandleResponse(response);

        // Assert
        @event.Request.Url.Should().Be("http://localhost/test/path?query=string"); // No admin:1234
        @event.Request.Data.Should().BeNull();
        var responseContext = @event.Contexts[Response.Type] as Response;
        responseContext?.Data.Should().BeNull();
    }

    [Fact]
    public void HandleResponse_Error_CaptureRequestAndResponse()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            SendDefaultPii = true
        };

        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, options);

        var query = ValidQuery;
        var url = "http://foo/bar/hello";
        var response = PreconditionFailedResponse();
        response.RequestMessage = SentryGraphQlTestHelpers.GetRequestQuery(query, url);
        var requestContent = new GraphQLRequestContent(SentryGraphQlTestHelpers.WrapRequestContent(query));
        response.RequestMessage!.SetFused(requestContent);
        response.Headers.Add("myHeader", "myValue");

        SentryEvent @event = null;
        hub.CaptureEvent(
            Arg.Do<SentryEvent>(e => @event = e),
            hint: Arg.Any<SentryHint>()
            );

        // Act
        sut.HandleResponse(response);

        // Assert
        using (new AssertionScope())
        {
            @event.Should().NotBeNull();

            // Ensure the mechanism is set
            @event.Exception?.Data[Mechanism.MechanismKey].Should().Be(SentryGraphQLHttpFailedRequestHandler.MechanismType);
            @event.Exception?.Data[Mechanism.HandledKey].Should().Be(false);

            // Ensure the request properties are captured
            @event.Request.Method.Should().Be(HttpMethod.Post.ToString());
            @event.Request.Url.Should().Be(url);
            @event.Request.ApiTarget.Should().Be("graphql");
            @event.Request.Data.Should().Be(SentryGraphQlTestHelpers.WrapRequestContent(query));

            // Ensure the response context is captured
            @event.Contexts.Should().Contain(x => x.Key == Response.Type && x.Value is Response);

            var responseContext = @event.Contexts[Response.Type] as Response;
            responseContext?.StatusCode.Should().Be((short)response.StatusCode);
            responseContext?.BodySize.Should().Be(response.Content.Headers.ContentLength);
            responseContext?.Data?.ToString().Should().Be(
                SentryGraphQlTestHelpers.ErrorContent("Bad query", "BAD_OP").ReadAsJson().ToString()
                );

            @event.Contexts.Response.Headers.Should().ContainKey("myHeader");
            @event.Contexts.Response.Headers.Should().ContainValue("myValue");

            // The fingerprints field should be set to ["$operationName", "$operationType", "$statusCode"].
            @event.Fingerprint.Should().BeEquivalentTo(
                "getAllNotes", "query", $"{(int)HttpStatusCode.PreconditionFailed}"
                );
        }
    }

    [Fact]
    public void HandleResponse_Error_ResponseAsHint()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var options = new SentryOptions
        {
            CaptureFailedRequests = true
        };
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, options);

        var response = PreconditionFailedResponse(); // This is in the range
        response.RequestMessage = SentryGraphQlTestHelpers.GetRequestQuery(ValidQuery);

        // Act
        SentryHint hint = null;
        hub.CaptureEvent(
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
