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
            Dsn = ValidDsn
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
            FailedRequestTargets = new List<StringOrRegex> { "http://foo/" }
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
    public void HandleResponse_Error_FiltersCookies()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            SendDefaultPii = true
        };
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, options);

        var response = PreconditionFailedResponse();
        response.RequestMessage = SentryGraphQlTestHelpers.GetRequestQuery(ValidQuery, "http://foo/bar/hello");
        response.RequestMessage!.SetFused(new GraphQLRequestContent(SentryGraphQlTestHelpers.WrapRequestContent(ValidQuery)));
        response.RequestMessage.Headers.Add("Cookie", "user_session=abc; theme=dark-mode;opaque-token");
        response.Headers.Add("Cookie", "csrf_token=abc; lang=en");

        // Act
        SentryEvent @event = null;
        hub.CaptureEvent(Arg.Do<SentryEvent>(e => @event = e), hint: Arg.Any<SentryHint>());
        sut.HandleResponse(response);

        // Assert
        @event.Request.Cookies.Should().Be("user_session=[Filtered]; theme=dark-mode; [Filtered]");
        @event.Contexts.Response.Cookies.Should().Be("csrf_token=[Filtered]; lang=en");
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
    public void HandleResponse_ErrorWithPii_FiltersSensitiveHeaderValues()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, new SentryOptions
        {
            CaptureFailedRequests = true,
            SendDefaultPii = true
        });
        var response = ResponseWithSensitiveHeaders();

        // Act
        SentryEvent @event = null;
        hub.CaptureEvent(Arg.Do<SentryEvent>(e => @event = e), hint: Arg.Any<SentryHint>());
        sut.HandleResponse(response);

        // Assert
        using (new AssertionScope())
        {
            @event.Request.Headers.Should().Equal(new Dictionary<string, string>
            {
                { "Authorization", "[Filtered]" },
                { "X-Api-Key", "[Filtered]" },
                { "Cookie", "[Filtered]" },
                { "Accept", "text/plain" }
            });
            @event.Contexts.Response.Headers.Should().Equal(new Dictionary<string, string>
            {
                { "Set-Cookie", "[Filtered]" },
                { "X-Auth-Token", "[Filtered]" },
                { "myHeader", "myValue" }
            });
        }
    }

    [Fact]
    public void HandleResponse_ErrorNoPii_DoesNotCaptureSensitiveHeaders()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, new SentryOptions
        {
            CaptureFailedRequests = true,
            SendDefaultPii = false
        });
        var response = ResponseWithSensitiveHeaders();

        // Act
        SentryEvent @event = null;
        hub.CaptureEvent(Arg.Do<SentryEvent>(e => @event = e), hint: Arg.Any<SentryHint>());
        sut.HandleResponse(response);

        // Assert
        using (new AssertionScope())
        {
            @event.Request.Headers.Should().BeEmpty();
            @event.Contexts.Response.Headers.Should().BeEmpty();
        }
    }

    private HttpResponseMessage ResponseWithSensitiveHeaders()
    {
        var request = SentryGraphQlTestHelpers.GetRequestQuery(ValidQuery, "http://foo/bar");
        request.Headers.Add("Authorization", "Bearer secret");
        request.Headers.Add("X-Api-Key", "secret");
        request.Headers.Add("Cookie", "name=secret");
        request.Headers.Add("Accept", "text/plain");

        var response = PreconditionFailedResponse();
        response.RequestMessage = request;
        response.Headers.Add("Set-Cookie", ["first=secret", "second=secret"]);
        response.Headers.Add("X-Auth-Token", "secret");
        response.Headers.Add("myHeader", "myValue");
        return response;
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
