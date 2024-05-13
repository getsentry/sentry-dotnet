using Sentry.Internal.OpenTelemetry;

namespace Sentry.Tests;

/*
 * NOTE: All tests should be done for both asynchronous `SendAsync` and synchronous `Send` methods.
 * TODO: Find a way to consolidate these tests cleanly.
 */

public class SentryHttpMessageHandlerTests : SentryMessageHandlerTests
{
    [Fact]
    public async Task SendAsync_SentryTraceHeaderNotSet_SetsHeader_ByDefault()
    {
        // Arrange
        var hub = Substitute.For<IHub>();

        hub.GetTraceHeader().ReturnsForAnyArgs(
            SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"));

        using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        // Act
        await client.GetAsync("https://localhost/");

        using var request = innerHandler.GetRequests().Single();

        // Assert
        request.Headers.Should().Contain(h =>
            h.Key == "sentry-trace" &&
            string.Concat(h.Value) == "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0");
    }

    [Fact]
    public async Task SendAsync_SentryTraceHeaderNotSet_SetsHeader_WhenUrlMatchesPropagationOptions()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var failedRequestHandler = Substitute.For<ISentryFailedRequestHandler>();
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<SubstringOrRegexPattern>
            {
                new("localhost")
            }
        };

        hub.GetTraceHeader().ReturnsForAnyArgs(
            SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"));

        using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
        using var sentryHandler = new SentryHttpMessageHandler(hub, options, innerHandler, failedRequestHandler);
        using var client = new HttpClient(sentryHandler);

        // Act
        await client.GetAsync("https://localhost/");

        using var request = innerHandler.GetRequests().Single();

        // Assert
        request.Headers.Should().Contain(h =>
            h.Key == "sentry-trace" &&
            string.Concat(h.Value) == "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0");
    }

    [Fact]
    public async Task SendAsync_SentryTraceHeaderNotSet_DoesntSetHeader_WhenUrlDoesntMatchesPropagationOptions()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var failedRequestHandler = Substitute.For<ISentryFailedRequestHandler>();
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<SubstringOrRegexPattern>
            {
                new("foo")
            }
        };

        hub.GetTraceHeader().ReturnsForAnyArgs(
            SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"));

        using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
        using var sentryHandler = new SentryHttpMessageHandler(hub, options, innerHandler, failedRequestHandler);
        using var client = new HttpClient(sentryHandler);

        // Act
        await client.GetAsync("https://localhost/");

        using var request = innerHandler.GetRequests().Single();

        // Assert
        request.Headers.Should().NotContain(h => h.Key == "sentry-trace");
    }

    [Fact]
    public async Task SendAsync_SentryTraceHeaderAlreadySet_NotOverwritten()
    {
        // Arrange
        var hub = Substitute.For<IHub>();

        hub.GetTraceHeader().ReturnsForAnyArgs(
            SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"));

        using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        client.DefaultRequestHeaders.Add("sentry-trace", "foobar");

        // Act
        await client.GetAsync("https://localhost/");

        using var request = innerHandler.GetRequests().Single();

        // Assert
        request.Headers.Should().Contain(h =>
            h.Key == "sentry-trace" &&
            string.Concat(h.Value) == "foobar");
    }

    [Fact]
    public async Task SendAsync_TransactionOnScope_StartsChildSpan()
    {
        // Arrange
        var hub = _fixture.GetHub();
        var transaction = hub.StartTransaction("foo", "bar");
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        using var innerHandler = new FakeHttpMessageHandler();
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        // Act
        await client.GetAsync("https://localhost/");

        // Assert
        transaction.Spans.Should().Contain(span =>
            span.Operation == "http.client" &&
            span.Description == "GET https://localhost/" &&
            span.IsFinished);
    }

    [Fact]
    public async Task SendAsync_NoTransactionOnScope_StartsTransaction()
    {
        // Arrange
        SentryTransaction received = null;
        _fixture.Client.CaptureTransaction(
            Arg.Do<SentryTransaction>(t => received = t),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>()
        );
        var hub = _fixture.GetHub();

        using var innerHandler = new FakeHttpMessageHandler();
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        // Act
        await client.GetAsync("https://localhost/");

        // Assert
        received.Should().NotBeNull();
        using (new AssertionScope())
        {
            received.Name.Should().Be("GET https://localhost/");
            received.Operation.Should().Be("http.client");
            received.Description.Should().Be("GET https://localhost/");
            received.IsFinished.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SendAsync_ExceptionThrown_ExceptionLinkedToSpan()
    {
        // Arrange
        var hub = _fixture.GetHub();

        var exception = new Exception();

        using var innerHandler = new FakeHttpMessageHandler(() => throw exception);
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        // Act
        await Assert.ThrowsAsync<Exception>(() => client.GetAsync("https://localhost/"));

        // Assert
        hub.ExceptionToSpanMap.TryGetValue(exception, out var span).Should().BeTrue();
        span.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_Executed_BreadcrumbCreated()
    {
        // Arrange
        var scope = new Scope();
        var hub = Substitute.For<IHub>();
        hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(c => c.Arg<Action<Scope>>()(scope));

        var url = "https://localhost/";

        var urlKey = "url";
        var methodKey = "method";
        var statusKey = "status_code";
        var expectedBreadcrumbData = new Dictionary<string, string>
        {
            {urlKey, url},
            {methodKey, "GET"},
            {statusKey, "200"}
        };
        var expectedType = "http";
        var expectedCategory = "http";
        using var sentryHandler = new SentryHttpMessageHandler(hub);
        sentryHandler.InnerHandler = new FakeHttpMessageHandler(); // No reason to reach the Internet here
        using var client = new HttpClient(sentryHandler);

        // Act
        await client.GetAsync(url);
        var breadcrumbGenerated = scope.Breadcrumbs.First();

        // Assert
        Assert.Equal(expectedType, breadcrumbGenerated.Type);
        Assert.Equal(expectedCategory, breadcrumbGenerated.Category);
        Assert.NotNull(breadcrumbGenerated.Data);

        Assert.True(breadcrumbGenerated.Data.ContainsKey(urlKey));
        Assert.Equal(expectedBreadcrumbData[urlKey], breadcrumbGenerated.Data[urlKey]);

        Assert.True(breadcrumbGenerated.Data.ContainsKey(methodKey));
        Assert.Equal(expectedBreadcrumbData[methodKey], breadcrumbGenerated.Data[methodKey]);

        Assert.True(breadcrumbGenerated.Data.ContainsKey(statusKey));
        Assert.Equal(expectedBreadcrumbData[statusKey], breadcrumbGenerated.Data[statusKey]);
    }

    [Fact]
    public async Task SendAsync_Executed_FailedRequestsCaptured()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var failedRequestHandler = Substitute.For<ISentryFailedRequestHandler>();
        var options = new SentryOptions();
        var url = "https://localhost/";

        using var innerHandler = new FakeHttpMessageHandler();
        using var sentryHandler = new SentryHttpMessageHandler(hub, options, innerHandler, failedRequestHandler);
        using var client = new HttpClient(sentryHandler);

        // Act
        await client.GetAsync(url);

        // Assert
        failedRequestHandler.Received(1).HandleResponse(Arg.Any<HttpResponseMessage>());
    }

    [Fact]
    public void ProcessRequest_SetsSpanData()
    {
        // Arrange
        var hub = _fixture.GetHub();
        using var innerHandler = new FakeHttpMessageHandler();
        var sut = new SentryHttpMessageHandler(hub, _fixture.Options, innerHandler);

        var method = "GET";
        var host = "example.com";
        var url = $"https://{host}/graphql";
        var uri = new Uri(url);
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        // Act
        var returnedSpan = sut.ProcessRequest(request, method, url);

        // Assert
        returnedSpan.Should().NotBeNull();
        returnedSpan!.Operation.Should().Be("http.client");
        returnedSpan.Description.Should().Be($"{method} {url}");
        returnedSpan.Extra.Should().Contain(kvp =>
            kvp.Key == OtelSemanticConventions.AttributeHttpRequestMethod &&
            Equals(kvp.Value, method)
        );
        returnedSpan.Extra.Should().Contain(kvp =>
            kvp.Key == OtelSemanticConventions.AttributeServerAddress &&
            Equals(kvp.Value, host)
        );
    }

    [Fact]
    public void HandleResponse_SetsSpanData()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var failedRequestHandler = Substitute.For<ISentryFailedRequestHandler>();
        var status = HttpStatusCode.OK;
        var response = new HttpResponseMessage(status);
        var method = "POST";
        var url = "https://example.com/";
        var sut = new SentryHttpMessageHandler(hub, null, null, failedRequestHandler);

        var transaction = new TransactionTracer(hub, "foo", "bar");
        var span = transaction.StartChild("http.client");

        // Act
        sut.HandleResponse(response, span, method, url);

        // Assert
        span.Should().NotBeNull();
        span.Extra.Should().ContainKey(OtelSemanticConventions.AttributeHttpResponseStatusCode);
        span.Extra[OtelSemanticConventions.AttributeHttpResponseStatusCode].Should().Be((int)status);
    }

#if NET5_0_OR_GREATER
    [Fact]
    public void Send_SentryTraceHeaderNotSet_SetsHeader_ByDefault()
    {
        // Arrange
        var hub = Substitute.For<IHub>();

        hub.GetTraceHeader().ReturnsForAnyArgs(
            SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"));

        using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        // Act
        client.Get("https://localhost/");

        using var request = innerHandler.GetRequests().Single();

        // Assert
        request.Headers.Should().Contain(h =>
            h.Key == "sentry-trace" &&
            string.Concat(h.Value) == "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0");
    }

    [Fact]
    public void Send_SentryTraceHeaderNotSet_SetsHeader_WhenUrlMatchesPropagationOptions()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var failedRequestHandler = Substitute.For<ISentryFailedRequestHandler>();
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<SubstringOrRegexPattern>
            {
                new("localhost")
            }
        };

        hub.GetTraceHeader().ReturnsForAnyArgs(
            SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"));

        using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
        using var sentryHandler = new SentryHttpMessageHandler(hub, options, innerHandler, failedRequestHandler);
        using var client = new HttpClient(sentryHandler);

        // Act
        client.Get("https://localhost/");

        using var request = innerHandler.GetRequests().Single();

        // Assert
        request.Headers.Should().Contain(h =>
            h.Key == "sentry-trace" &&
            string.Concat(h.Value) == "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0");
    }

    [Fact]
    public void Send_SentryTraceHeaderNotSet_DoesntSetHeader_WhenUrlDoesntMatchesPropagationOptions()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var failedRequestHandler = Substitute.For<ISentryFailedRequestHandler>();
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<SubstringOrRegexPattern>
            {
                new("foo")
            }
        };

        hub.GetTraceHeader().ReturnsForAnyArgs(
            SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"));

        using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
        using var sentryHandler = new SentryHttpMessageHandler(hub, options, innerHandler, failedRequestHandler);
        using var client = new HttpClient(sentryHandler);

        // Act
        client.Get("https://localhost/");

        using var request = innerHandler.GetRequests().Single();

        // Assert
        request.Headers.Should().NotContain(h => h.Key == "sentry-trace");
    }

    [Fact]
    public void Send_SentryTraceHeaderAlreadySet_NotOverwritten()
    {
        // Arrange
        var hub = Substitute.For<IHub>();

        hub.GetTraceHeader().ReturnsForAnyArgs(
            SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"));

        using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        client.DefaultRequestHeaders.Add("sentry-trace", "foobar");

        // Act
        client.Get("https://localhost/");

        using var request = innerHandler.GetRequests().Single();

        // Assert
        request.Headers.Should().Contain(h =>
            h.Key == "sentry-trace" &&
            string.Concat(h.Value) == "foobar");
    }

    [Fact]
    public void Send_TransactionOnScope_StartsNewSpan()
    {
        // Arrange
        var hub = _fixture.GetHub();
        var transaction = hub.StartTransaction("foo", "bar");
        hub.ConfigureScope(scope => scope.Transaction = transaction);

        using var innerHandler = new FakeHttpMessageHandler();
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        // Act
        client.Get("https://localhost/");

        // Assert
        transaction.Spans.Should().Contain(span =>
            span.Operation == "http.client" &&
            span.Description == "GET https://localhost/" &&
            span.IsFinished);
    }

    [Fact]
    public void Send_ExceptionThrown_ExceptionLinkedToSpan()
    {
        // Arrange
        var hub = _fixture.GetHub();

        var exception = new Exception();

        using var innerHandler = new FakeHttpMessageHandler(() => throw exception);
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        // Act
        Assert.Throws<Exception>(() => client.Get("https://localhost/"));

        // Assert
        hub.ExceptionToSpanMap.TryGetValue(exception, out var span).Should().BeTrue();
        span.Should().NotBeNull();
    }

    [Fact]
    public void Send_Executed_BreadcrumbCreated()
    {
        // Arrange
        var scope = new Scope();
        var hub = Substitute.For<IHub>();
        hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(c => c.Arg<Action<Scope>>()(scope));

        var url = "https://localhost/";

        var urlKey = "url";
        var methodKey = "method";
        var statusKey = "status_code";
        var expectedBreadcrumbData = new Dictionary<string, string>
        {
            {urlKey, url},
            {methodKey, "GET"},
            {statusKey, "200"}
        };
        var expectedType = "http";
        var expectedCategory = "http";
        using var sentryHandler = new SentryHttpMessageHandler(hub);
        sentryHandler.InnerHandler = new FakeHttpMessageHandler(); // No reason to reach the Internet here
        using var client = new HttpClient(sentryHandler);

        // Act
        client.Get(url);
        var breadcrumbGenerated = scope.Breadcrumbs.First();

        // Assert
        Assert.Equal(expectedType, breadcrumbGenerated.Type);
        Assert.Equal(expectedCategory, breadcrumbGenerated.Category);
        Assert.NotNull(breadcrumbGenerated.Data);

        Assert.True(breadcrumbGenerated.Data.ContainsKey(urlKey));
        Assert.Equal(expectedBreadcrumbData[urlKey], breadcrumbGenerated.Data[urlKey]);

        Assert.True(breadcrumbGenerated.Data.ContainsKey(methodKey));
        Assert.Equal(expectedBreadcrumbData[methodKey], breadcrumbGenerated.Data[methodKey]);

        Assert.True(breadcrumbGenerated.Data.ContainsKey(statusKey));
        Assert.Equal(expectedBreadcrumbData[statusKey], breadcrumbGenerated.Data[statusKey]);
    }

    [Fact]
    public void Send_Executed_FailedRequestsCaptured()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var failedRequestHandler = Substitute.For<ISentryFailedRequestHandler>();
        var options = new SentryOptions();
        var url = "https://localhost/";

        using var innerHandler = new FakeHttpMessageHandler();
        using var sentryHandler = new SentryHttpMessageHandler(hub, options, innerHandler, failedRequestHandler);
        using var client = new HttpClient(sentryHandler);

        // Act
        client.Get(url);

        // Assert
        failedRequestHandler.Received(1).HandleResponse(Arg.Any<HttpResponseMessage>());
    }
#endif
}
