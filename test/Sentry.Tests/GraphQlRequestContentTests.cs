namespace Sentry.Tests;

public class GraphQlRequestContentTests
{
    private const string RequestBody =
        """{"query":"query getNote($id: ID!) { note(id: $id) { id } }","operationName":"getNote","variables":{"id":"café"}}""";

    [Fact]
    public void FilterRequestContent_DocumentAndVariables_ReturnsBodyUnchanged()
    {
        var sut = new GraphQLRequestContent(RequestBody);

        var result = sut.FilterRequestContent(includeDocument: true, includeVariables: true);

        result.Should().BeSameAs(RequestBody);
    }

    [Fact]
    public void FilterRequestContent_NeitherDocumentNorVariables_ReturnsNull()
    {
        var sut = new GraphQLRequestContent(RequestBody);

        var result = sut.FilterRequestContent(includeDocument: false, includeVariables: false);

        result.Should().BeNull();
    }

    [Fact]
    public void FilterRequestContent_DocumentOnly_RemovesVariables()
    {
        var sut = new GraphQLRequestContent(RequestBody);

        var result = sut.FilterRequestContent(includeDocument: true, includeVariables: false);

        result.Should().Be("""{"query":"query getNote($id: ID!) { note(id: $id) { id } }","operationName":"getNote"}""");
    }

    [Fact]
    public void FilterRequestContent_VariablesOnly_RemovesDocument()
    {
        var sut = new GraphQLRequestContent(RequestBody);

        var result = sut.FilterRequestContent(includeDocument: false, includeVariables: true);

        result.Should().Be("""{"operationName":"getNote","variables":{"id":"café"}}""");
    }

    [Fact]
    public void FilterRequestContent_BatchedRequests_FiltersEachRequest()
    {
        var sut = new GraphQLRequestContent("""[{"query":"{ a }","variables":{"x":1}},{"query":"{ b }"}]""");

        var result = sut.FilterRequestContent(includeDocument: false, includeVariables: true);

        result.Should().Be("""[{"variables":{"x":1}},{}]""");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("\"query\"")]
    [InlineData("""[{"query":"{ a }"},1]""")]
    [InlineData("""{"\ud83d":1,"query":"{ a }"}""")]
    public void FilterRequestContent_NotAGraphQLRequest_ReturnsNullAndLogs(string requestBody)
    {
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        var sut = new GraphQLRequestContent(requestBody, options);

        var result = sut.FilterRequestContent(includeDocument: true, includeVariables: false);

        using (new AssertionScope())
        {
            result.Should().BeNull();
            logger.Entries.Should().Contain(entry =>
                entry.Level == SentryLevel.Debug
                && entry.Message.StartsWith("Dropping the GraphQL request body")
                && entry.Args.Contains("variables"));
        }
    }

    [Fact]
    public void FilterRequestContent_NoContent_ReturnsNull()
    {
        var sut = new GraphQLRequestContent(null);

        var result = sut.FilterRequestContent(includeDocument: true, includeVariables: false);

        result.Should().BeNull();
    }
}
