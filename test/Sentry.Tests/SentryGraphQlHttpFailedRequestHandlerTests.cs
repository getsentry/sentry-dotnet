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
        var options = new SentryOptions
        {
            CaptureFailedRequests = true,
            SendDefaultPii = true
        };

        var response = PreconditionFailedResponse();
        response.RequestMessage = SentryGraphQlTestHelpers.GetRequestQuery(ValidQuery, "http://foo/bar/hello");
        response.RequestMessage!.SetFused(new GraphQLRequestContent(SentryGraphQlTestHelpers.WrapRequestContent(ValidQuery)));
        response.RequestMessage.Headers.Add("Cookie", "user_session=abc; theme=dark-mode;opaque-token");
        response.Headers.Add("Set-Cookie", ["csrf_token=abc; Path=/; HttpOnly", "lang=en; Max-Age=3600", "opaque-token; Secure"]);

        // Act
        var @event = CaptureEvent(options, response);

        // Assert
        @event.Request.Cookies.Should().Be("user_session=[Filtered]; theme=dark-mode; [Filtered]");
        @event.Contexts.Response.Cookies.Should().Be("csrf_token=[Filtered]; lang=en; [Filtered]");
    }

    [Fact]
    public void HandleResponse_ErrorNoPii_SkipsCookies()
    {
        var @event = CaptureEvent(new SentryOptions { CaptureFailedRequests = true }, ResponseWithCookies());

        using (new AssertionScope())
        {
            @event.Request.Cookies.Should().BeNull();
            @event.Contexts.Response.Cookies.Should().BeNull();
        }
    }

    [Fact]
    public void HandleResponse_ResponseCookieHeader_IsNotCollectedAsResponseCookies()
    {
        var response = PreconditionFailedResponse();
        response.RequestMessage = SentryGraphQlTestHelpers.GetRequestQuery(ValidQuery, "http://foo/bar");
        response.Headers.Add("Cookie", "lang=en");

        var @event = CaptureEvent(new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = true }, response);

        @event.Contexts.Response.Cookies.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandleResponse_CookiesConfigured_IgnoresSendDefaultPii(bool sendDefaultPii)
    {
        var options = new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection =>
            dataCollection.Cookies = sendDefaultPii ? KeyValueCollectionBehavior.Off : KeyValueCollectionBehavior.DenyList("theme"));

        var @event = CaptureEvent(options, ResponseWithCookies());

        using (new AssertionScope())
        {
            if (sendDefaultPii)
            {
                @event.Request.Cookies.Should().BeNull();
                @event.Contexts.Response.Cookies.Should().BeNull();
            }
            else
            {
                @event.Request.Cookies.Should().Be("user_session=[Filtered]; theme=[Filtered]");
                @event.Contexts.Response.Cookies.Should().Be("theme=[Filtered]; lang=en");
            }
        }
    }

    private HttpResponseMessage ResponseWithCookies()
    {
        var response = PreconditionFailedResponse();
        response.RequestMessage = SentryGraphQlTestHelpers.GetRequestQuery(ValidQuery, "http://foo/bar");
        response.RequestMessage.Headers.Add("Cookie", "user_session=abc; theme=dark-mode");
        response.Headers.Add("Set-Cookie", ["theme=light-mode; Path=/", "lang=en; HttpOnly"]);
        return response;
    }

    [Fact]
    public void HandleResponse_Error_DontSendPii()
    {
        // Arrange
        var options = new SentryOptions
        {
            CaptureFailedRequests = true
        };

        var response = PreconditionFailedResponse();
        var uri = new Uri("http://admin:1234@localhost/test/path?query=string#fragment");
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

        // Act
        var @event = CaptureEvent(options, response);

        // Assert
        @event.Request.Url.Should().Be("http://localhost/test/path?query=string"); // No admin:1234
        @event.Request.Data.Should().BeNull();
        var responseContext = @event.Contexts[Response.Type] as Response;
        responseContext?.Data.Should().BeNull();
    }

    [Theory]
    [InlineData(false, "https://example.com/api/users")]
    [InlineData(true, "https://example.com/api/users#top")]
    public void HandleResponse_UrlQueryParamsOff_DropsQuery(bool sendDefaultPii, string expectedUrl)
    {
        var options = new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection => dataCollection.UrlQueryParams = KeyValueCollectionBehavior.Off);
        var response = PreconditionFailedResponse();
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://example.com/api/users?q=abc&page=5#top"));

        var @event = CaptureEvent(options, response);

        using (new AssertionScope())
        {
            @event.Request.Url.Should().Be(expectedUrl);
            @event.Request.QueryString.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandleResponse_UrlQueryParamsAllowList_FiltersOtherValues(bool sendDefaultPii)
    {
        var options = new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection =>
            dataCollection.UrlQueryParams = KeyValueCollectionBehavior.AllowList("page", "token"));
        var response = PreconditionFailedResponse();
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://example.com/api/users?token=abc&q=abc&page=5"));

        var @event = CaptureEvent(options, response);

        using (new AssertionScope())
        {
            @event.Request.Url.Should().Be("https://example.com/api/users?token=[Filtered]&q=[Filtered]&page=5");
            @event.Request.QueryString.Should().Be("?token=[Filtered]&q=[Filtered]&page=5");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandleResponse_WithoutDataCollection_AttachesBodiesOnlyWithSendDefaultPii(bool sendDefaultPii)
    {
        var options = new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = sendDefaultPii };

        var @event = CaptureEvent(options, ResponseWithVariables());

        using (new AssertionScope())
        {
            @event.Request.Data.Should().Be(sendDefaultPii ? RequestBodyWithVariables : null);
            (@event.Contexts.Response.Data?.ToString()).Should().Be(sendDefaultPii ? ErrorResponseJson : null);
        }
    }

    [Theory]
    [InlineData(false, true, true, RequestBodyWithVariables)]
    [InlineData(false, true, false, """{"query":"query getNote($id: ID!) { note(id: $id) { id } }"}""")]
    [InlineData(false, false, true, """{"variables":{"id":"secret"}}""")]
    [InlineData(false, false, false, null)]
    [InlineData(true, true, true, RequestBodyWithVariables)]
    [InlineData(true, true, false, """{"query":"query getNote($id: ID!) { note(id: $id) { id } }"}""")]
    [InlineData(true, false, true, """{"variables":{"id":"secret"}}""")]
    [InlineData(true, false, false, null)]
    public void HandleResponse_ConfiguredGraphQL_AttachesOnlyCollectedParts(
        bool sendDefaultPii, bool document, bool variables, string expectedData)
    {
        var options = new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.GraphQL.Document = document;
            dataCollection.GraphQL.Variables = variables;
        });

        var @event = CaptureEvent(options, ResponseWithVariables());

        using (new AssertionScope())
        {
            @event.Request.Data.Should().Be(expectedData);
            (@event.Contexts.Response.Data?.ToString()).Should().Be(ErrorResponseJson);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandleResponse_ConfiguredWithoutOutgoingRequestBody_DoesNotAttachRequestBody(bool sendDefaultPii)
    {
        var options = new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection =>
            dataCollection.HttpBodies = HttpBodyTypes.All & ~HttpBodyTypes.OutgoingRequest);

        var @event = CaptureEvent(options, ResponseWithVariables());

        using (new AssertionScope())
        {
            @event.Request.Data.Should().BeNull();
            (@event.Contexts.Response.Data?.ToString()).Should().Be(ErrorResponseJson);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandleResponse_ConfiguredWithoutIncomingResponseBody_DoesNotAttachResponseBody(bool sendDefaultPii)
    {
        var options = new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection =>
            dataCollection.HttpBodies = HttpBodyTypes.All & ~HttpBodyTypes.IncomingResponse);

        var @event = CaptureEvent(options, ResponseWithVariables());

        using (new AssertionScope())
        {
            @event.Request.Data.Should().Be(RequestBodyWithVariables);
            @event.Contexts.Response.Data.Should().BeNull();
        }
    }

    private const string RequestBodyWithVariables =
        """{"query":"query getNote($id: ID!) { note(id: $id) { id } }","variables":{"id":"secret"}}""";

    private static string ErrorResponseJson =>
        SentryGraphQlTestHelpers.ErrorContent("Bad query", "BAD_OP").ReadAsJson().ToString();

    private HttpResponseMessage ResponseWithVariables()
    {
        var response = PreconditionFailedResponse();
        response.RequestMessage = SentryGraphQlTestHelpers.GetRequest(new StringContent(RequestBodyWithVariables), "http://foo/graphql");
        response.RequestMessage.SetFused(new GraphQLRequestContent(RequestBodyWithVariables));
        return response;
    }

    private static SentryEvent CaptureEvent(SentryOptions options, HttpResponseMessage response)
    {
        var hub = Substitute.For<IHub>();
        SentryEvent @event = null;
        hub.CaptureEvent(Arg.Do<SentryEvent>(e => @event = e), hint: Arg.Any<SentryHint>());
        new SentryGraphQLHttpFailedRequestHandler(hub, options).HandleResponse(response);
        return @event;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandleResponse_SensitiveQueryParameter_FiltersUrlAndQueryString(bool sendDefaultPii)
    {
        var hub = Substitute.For<IHub>();
        var options = new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = sendDefaultPii };
        var sut = new SentryGraphQLHttpFailedRequestHandler(hub, options);

        var response = PreconditionFailedResponse();
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://example.com/api/users?token=abc123&q=a%20b%26c&page=5"));

        SentryEvent @event = null;
        hub.CaptureEvent(Arg.Do<SentryEvent>(e => @event = e), hint: Arg.Any<SentryHint>());
        sut.HandleResponse(response);

        @event.Request.Url.Should().Be("https://example.com/api/users?token=[Filtered]&q=a%20b%26c&page=5");
        @event.Request.QueryString.Should().Be("?token=[Filtered]&q=a%20b%26c&page=5");
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

    [Fact]
    public void HandleResponse_HeadersConfiguredWithoutSendDefaultPii_CollectsHeaders()
    {
        var options = new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = false };
        options.ConfigureDataCollection(_ => { });

        var @event = CaptureEvent(options, ResponseWithSensitiveHeaders());

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
    public void HandleResponse_HeadersOffWithSendDefaultPii_SkipsHeaders()
    {
        var options = new SentryOptions { CaptureFailedRequests = true, SendDefaultPii = true };
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.Off;
            dataCollection.HttpHeaders.Response = KeyValueCollectionBehavior.Off;
        });

        var @event = CaptureEvent(options, ResponseWithSensitiveHeaders());

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
