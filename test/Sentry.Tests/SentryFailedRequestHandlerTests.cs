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
    public void HandleResponse_Capture_Mechanism()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public void HandleResponse_Capture_Hints()
    {
        throw new NotImplementedException();

        //verify(fixture.hub).captureEvent(
        //    any(),
        //    check<Hint> {
        //    assertNotNull(it.get(TypeCheckHint.OKHTTP_REQUEST))
        //        assertNotNull(it.get(TypeCheckHint.OKHTTP_RESPONSE))
        //    }
        //)
    }

    [Fact]
    public void HandleResponse_Capture_RequestAndResponse()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true
        };
        var sut = GetSut(options);

        var url = "http://foo/bar/hello";
        var queryString = "myQuery=myValue";
        var fragment = "myFragment";
        var response = InternalServerErrorResponse(); // This is in the range
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, $"{url}?{queryString}#{fragment}");

        // Act
        SentryEvent @event = null;
        _hub.CaptureEvent(Arg.Do<SentryEvent>(e => @event = e));
        sut.HandleResponse(response);

        // Assert
        using (new AssertionScope())
        {
            @event.Should().NotBeNull();

            @event.Request.Method.Should().Be(HttpMethod.Post.ToString());
            @event.Request.Url.Should().Be(url);
            @event.Request.QueryString.Should().Be(queryString);
            // Not sure how we want to handle fragments in .NET... potentially we add a Fragment
            // property to the Request class, but that might be a breaking change.
            // @event.Request.Other.Should().Contain(other => other.Key == "fragment" && other.Value == fragment);

            throw new NotImplementedException("Test doesn't yet check for the response is set") ;
            //@event.Contexts.Should().Contain(x =>
            //    x.Key == "response"
            //    && (
            //        x.Value is JsonElement
            //        || x.Value is string)
            //    );
        }

        /*
            @Test
            fun `captures an error event with request and response fields set`() {
                val statusCode = 500
                val sut = fixture.getSut(
                    captureFailedRequests = true,
                    httpStatusCode = statusCode,
                    responseBody = "fail"
                )

                val request = getRequest(url = "/hello?myQuery=myValue#myFragment")
                val response = sut.newCall(request).execute()

                verify(fixture.hub).captureEvent(
                    check {
                        val sentryRequest = it.request!!
                        assertEquals("http://localhost:${fixture.server.port}/hello", sentryRequest.url)
                        assertEquals("myQuery=myValue", sentryRequest.queryString)
                        assertEquals("myFragment", sentryRequest.fragment)
                        assertEquals("GET", sentryRequest.method)

                        // because of isSendDefaultPii
                        assertNull(sentryRequest.headers)
                        assertNull(sentryRequest.cookies)

                        val sentryResponse = it.contexts.response!!
                        assertEquals(statusCode, sentryResponse.statusCode)
                        assertEquals(response.body!!.contentLength(), sentryResponse.bodySize)

                        // because of isSendDefaultPii
                        assertNull(sentryRequest.headers)
                        assertNull(sentryRequest.cookies)

                        assertTrue(it.throwable is SentryHttpClientException)
                    },
                    any<Hint>()
                )
            }
         */
    }

    [Fact]
    public void HandleResponse_Capture_RequestBodySize()
    {
        throw new NotImplementedException();

        /*
                @Test
                fun `captures an error event with request body size`() {
                    val sut = fixture.getSut(
                        captureFailedRequests = true,
                        httpStatusCode = 500,
                    )

                    val body = "fail"
                        .toRequestBody(
                            "text/plain"
                                .toMediaType()
                        )

                    sut.newCall(postRequest(body = body)).execute()

                    verify(fixture.hub).captureEvent(
                        check {
                            val sentryRequest = it.request!!
                            assertEquals(body.contentLength(), sentryRequest.bodySize)
                        },
                        any<Hint>()
                    )
                }
         */
    }

    [Fact]
    public void HandleResponse_Capture_Headers()
    {
        throw new NotImplementedException();

        /*
                @Test
                fun `captures an error event with headers`() {
                    val sut = fixture.getSut(
                        captureFailedRequests = true,
                        httpStatusCode = 500,
                        sendDefaultPii = true
                    )

                    sut.newCall(getRequest()).execute()

                    verify(fixture.hub).captureEvent(
                        check {
                            val sentryRequest = it.request!!
                            assertEquals("myValue", sentryRequest.headers!!["myHeader"])

                            val sentryResponse = it.contexts.response!!
                            assertEquals("myValue", sentryResponse.headers!!["myResponseHeader"])
                        },
                        any<Hint>()
                    )
                }  
         */
    }
}
