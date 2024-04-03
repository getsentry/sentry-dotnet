using Sentry.Internal.OpenTelemetry;

namespace Sentry.Tests;

/*
 * NOTE: All tests should be done for both asynchronous `SendAsync` and synchronous `Send` methods.
 * TODO: Find a way to consolidate these tests cleanly.
 */

public class SentryHttpMessageHandlerTests
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
    public async Task SendAsync_TransactionOnScope_StartsNewSpan()
    {
        // Arrange
        var hub = Substitute.For<IHub>();

        var transaction = new TransactionTracer(
            hub,
            "foo",
            "bar");

        hub.GetSpan().ReturnsForAnyArgs(transaction);

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
    public async Task SendAsync_ExceptionThrown_ExceptionLinkedToSpan()
    {
        // Arrange
        var hub = Substitute.For<IHub>();

        var transaction = new TransactionTracer(
            hub,
            "foo",
            "bar");

        hub.GetSpan().ReturnsForAnyArgs(transaction);

        var exception = new Exception();

        using var innerHandler = new FakeHttpMessageHandler(() => throw exception);
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        // Act
        await Assert.ThrowsAsync<Exception>(() => client.GetAsync("https://localhost/"));

        // Assert
        hub.Received(1).BindException(exception, Arg.Any<ISpan>()); // second argument is an implicitly created span
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
    [Obsolete("Obsolete")]
    public void ProcessRequest_SetsSpanData()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var parentSpan = Substitute.For<ISpan>();
        hub.GetSpan().Returns(parentSpan);
        var childSpan = Substitute.For<ISpan>();
        parentSpan.When(p => p.StartChild(Arg.Any<string>()))
            .Do(op => childSpan.Operation = op.Arg<string>());
        parentSpan.StartChild(Arg.Any<string>()).Returns(childSpan);
        var sut = new SentryHttpMessageHandler(hub, null);

        var method = "GET";
        var url = "http://example.com/graphql";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var returnedSpan = sut.ProcessRequest(request, method, url);

        // Assert
        returnedSpan.Should().NotBeNull();
        returnedSpan!.Operation.Should().Be("http.client");
        returnedSpan.Description.Should().Be($"{method} {url}");
        returnedSpan.Received(1).SetExtra(OtelSemanticConventions.AttributeHttpRequestMethod, method);
    }

    [Fact]
    [Obsolete("Obsolete")]
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
        var hub = Substitute.For<IHub>();

        var transaction = new TransactionTracer(
            hub,
            "foo",
            "bar");

        hub.GetSpan().ReturnsForAnyArgs(transaction);

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
        var hub = Substitute.For<IHub>();

        var transaction = new TransactionTracer(
            hub,
            "foo",
            "bar");

        hub.GetSpan().ReturnsForAnyArgs(transaction);

        var exception = new Exception();

        using var innerHandler = new FakeHttpMessageHandler(() => throw exception);
        using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
        using var client = new HttpClient(sentryHandler);

        // Act
        Assert.Throws<Exception>(() => client.Get("https://localhost/"));

        // Assert
        hub.Received(1).BindException(exception, Arg.Any<ISpan>()); // second argument is an implicitly created span
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
