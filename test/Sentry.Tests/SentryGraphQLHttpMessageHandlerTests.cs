namespace Sentry.Tests;

/*
 * NOTE: All tests should be done for both asynchronous `SendAsync` and synchronous `Send` methods.
 * TODO: Find a way to consolidate these tests cleanly.
 */

public class SentryGraphQLHttpMessageHandlerTests
{
    private readonly dynamic _validRequest = new
    {
        operationName = "getAllNotes",
        query = @"{
                 test {
                   foo,
                   bar
                 }
                }"
    };
    private StringContent ValidContent => new (JsonSerializer.Serialize(_validRequest), Encoding.UTF8,
        "application/json");

    [Fact]
    public void OnProcessRequest_UnpacksRequest()
    {
        // Arrange
        var request = new HttpRequestMessage { Content = ValidContent };
        var span = Substitute.For<ISpan>();
        var method = "POST";
        var url = "http://example.com/graphql";
        var sut = new SentryGraphQLHttpMessageHandler();

        // Act
        sut.OnProcessRequest(request, span, method, url);

        // Assert
        var graphqlInfo = request.With<SentryGraphQLHttpMessageHandler.GraphQLRequestInfo>();
        graphqlInfo.OperationName.Should().Be(_validRequest.operationName);
        graphqlInfo.OperationType.Should().Be("query");
        graphqlInfo.Query.Should().Be(_validRequest.query);
    }

    [Fact]
    public void GetBreadcrumb_ReturnsBreadcrumb()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var response = new HttpResponseMessage { StatusCode = HttpStatusCode.OK };
        var request = new HttpRequestMessage { Content = ValidContent };
        response.RequestMessage = request;
        var span = Substitute.For<ISpan>();

        var method = "POST";
        var url = "http://example.com/graphql";

        // Act
        var sut = new SentryGraphQLHttpMessageHandler(hub: hub);
        sut.OnProcessRequest(request, span, method, url); // DoAddBreadcrumb relies on this having been called first
        var breadcrumb = sut.GetBreadcrumb(response, span, method, url);

        // Assert
        breadcrumb.Should().NotBeNull();
        breadcrumb.Type.Should().Be("graphql");
        breadcrumb.Category.Should().Be("query");
        breadcrumb.Data.Should().BeEquivalentTo(new Dictionary<string, string>()
            {
                { "url",  url },
                { "method",  method },
                { "status_code",  ((int)response.StatusCode).ToString() },
                { "operation_name", _validRequest.operationName},
                { "operation_type", "query"},
            }
        );
    }

    [Fact]
    public void OnBeforeFinishSpan_SetsSpanStatusAndDescription()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var request = new HttpRequestMessage { Content = ValidContent };
        response.RequestMessage = request;
        var span = new TransactionTracer(hub, "fakeName", "fakeOperation");

        var method = "POST";
        var url = "http://example.com/graphql";

        // Act
        var sut = new SentryGraphQLHttpMessageHandler(hub: hub);
        sut.OnProcessRequest(request, span, method, url); // OnBeforeFinishSpan relies on this having been called first
        sut.OnBeforeFinishSpan(response, span, method, url);

        // Assert
        span.Status.Should().Be(SpanStatus.Ok);
        span.Description.Should().Be("getAllNotes query 200");
    }

}
