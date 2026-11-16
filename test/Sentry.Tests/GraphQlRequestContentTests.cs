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
    public void FilterRequestContent_NotAGraphQLRequest_ReturnsNull(string requestBody)
    {
        var sut = new GraphQLRequestContent(requestBody);

        var result = sut.FilterRequestContent(includeDocument: true, includeVariables: false);

        result.Should().BeNull();
    }

    [Fact]
    public void FilterRequestContent_NoContent_ReturnsNull()
    {
        var sut = new GraphQLRequestContent(null);

        var result = sut.FilterRequestContent(includeDocument: true, includeVariables: false);

        result.Should().BeNull();
    }
}
