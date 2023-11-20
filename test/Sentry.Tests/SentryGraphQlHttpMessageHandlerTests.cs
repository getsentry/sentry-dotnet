using Sentry.Internal.OpenTelemetry;

namespace Sentry.Tests;

/*
 * NOTE: All tests should be done for both asynchronous `SendAsync` and synchronous `Send` methods.
 * TODO: Find a way to consolidate these tests cleanly.
 */

public class SentryGraphQlHttpMessageHandlerTests
{
    private const string ValidQuery = "query getAllNotes { notes { id } }";
    private const string ValidResponse = @"{
    ""notes"": [
            {
            ""id"": 0
        },
        {
            ""id"": 1
        }
    ]
}";
    private StringContent ValidResponseContent => SentryGraphQlTestHelpers.ResponesContent(ValidResponse);

    [Fact]
    public void ProcessRequest_ExtractsGraphQlRequestContent()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var method = "POST";
        var url = "http://example.com/graphql";
        var sut = new SentryGraphQLHttpMessageHandler(hub, null);
        var query = ValidQuery;
        var request = SentryGraphQlTestHelpers.GetRequestQuery(query);

        // Act
        sut.ProcessRequest(request, method, url);

        // Assert
        var graphqlInfo = request.GetFused<GraphQLRequestContent>();
        graphqlInfo.OperationName.Should().Be("getAllNotes");
        graphqlInfo.OperationType.Should().Be("query");
        graphqlInfo.Query.Should().Be(query);
    }

    [Fact]
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
        var sut = new SentryGraphQLHttpMessageHandler(hub, null);

        var method = "POST";
        var url = "http://example.com/graphql";
        var query = ValidQuery;
        var request = SentryGraphQlTestHelpers.GetRequestQuery(query);

        // Act
        var returnedSpan = sut.ProcessRequest(request, method, url);

        // Assert
        returnedSpan.Should().NotBeNull();
        returnedSpan!.Operation.Should().Be("http.client");
        returnedSpan.Description.Should().Be($"{method} {url}");
        returnedSpan.Received(1).SetExtra(OtelSemanticConventions.AttributeHttpRequestMethod, method);
    }

    // [Theory]
    // [InlineData(ValidQuery)]
    [Fact]
    public void HandleResponse_AddsBreadcrumb()
    {
        // Arrange
        var method = "POST";
        var url = "http://foo/bar";

        var scope = new Scope();
        var hub = Substitute.For<IHub>();
        hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(c => c.Arg<Action<Scope>>()(scope));

        var query = ValidQuery;
        var request = SentryGraphQlTestHelpers.GetRequestQuery(query, url);
        var response = new HttpResponseMessage { Content = ValidResponseContent, StatusCode = HttpStatusCode.OK, RequestMessage = request};
        var wrappedQuery = SentryGraphQlTestHelpers.WrapRequestContent(query);
        request.SetFused(new GraphQLRequestContent(wrappedQuery));

        var options = new SentryOptions()
        {
            CaptureFailedRequests = true
        };
        var sut = new SentryGraphQLHttpMessageHandler(hub, options);

        // Act
        sut.HandleResponse(response, null, method, url);

        // Assert
        var breadcrumb = scope.Breadcrumbs.First();
        breadcrumb.Should().NotBeNull();
        breadcrumb.Type.Should().Be("graphql");
        breadcrumb.Category.Should().Be("query");
        breadcrumb.Data.Should().Contain("url", url);
        breadcrumb.Data.Should().Contain("method", method);
        breadcrumb.Data.Should().Contain("status_code", ((int)response.StatusCode).ToString());
        breadcrumb.Data.Should().Contain("request_body_size", SentryGraphQlTestHelpers.WrapRequestContent(query).Length.ToString());
        breadcrumb.Data.Should().Contain("response_body_size", response.Content.Headers.ContentLength?.ToString());
        breadcrumb.Data.Should().Contain("operation_name", "getAllNotes");
        breadcrumb.Data.Should().Contain("operation_type", "query");
    }

    [Fact]
    public void HandleResponse_SetsSpanData()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var status = HttpStatusCode.OK;
        var response = new HttpResponseMessage(status);
        var method = "POST";
        var url = "http://example.com/graphql";
        var request = SentryGraphQlTestHelpers.GetRequestQuery(ValidQuery, url);
        response.RequestMessage = request;
        hub.GetSpan().Returns(new TransactionTracer(hub, "test", "test"));
        var sut = new SentryGraphQLHttpMessageHandler(hub, null);

        // Act
        var span = sut.ProcessRequest(request, method, url); // HandleResponse relies on this having been called first
        sut.HandleResponse(response, span, method, url);

        // Assert
        span.Should().NotBeNull();
        span!.Status.Should().Be(SpanStatus.Ok);
        span.Description.Should().Be("getAllNotes query 200");
        span.Extra.Should().ContainKey(OtelSemanticConventions.AttributeHttpResponseStatusCode);
        span.Extra[OtelSemanticConventions.AttributeHttpResponseStatusCode].Should().Be((int)status);
    }
}
