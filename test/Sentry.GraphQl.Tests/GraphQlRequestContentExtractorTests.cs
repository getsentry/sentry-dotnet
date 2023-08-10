namespace Sentry.GraphQl.Tests;

public class GraphQlRequestContentExtractorTests
{
    private readonly Fixture _fixture;
    private const string ValidQuery = "query { notes { id } }";
    private const string ValidQueryWithName = "query getAllNotes { notes { id } }";
    private const string ValidShorthandQuery = "{ notes { id } }";
    private const string ValidMutation = "mutation saveSomething { id }";

    public GraphQlRequestContentExtractorTests()
    {
        _fixture = new Fixture();
    }

    private class Fixture
    {
        public SentryOptions Options => Substitute.For<SentryOptions>();

        public GraphQlRequestContentExtractor GetSut()
        {
            return new GraphQlRequestContentExtractor(Options);
        }
    }

    [Theory]
    [InlineData(ValidQuery, "query", "")]
    [InlineData(ValidShorthandQuery, "query", "")]
    [InlineData(ValidQueryWithName, "query", "getAllNotes")]
    [InlineData(ValidMutation, "mutation", "saveSomething")]
    public async Task ExtractContent_ValidQuery_UnpacksRequest(string query, string operationType, string operationName)
    {
        // Arrange
        var sut = _fixture.GetSut();
        var request = SentryGraphQlTestHelpers.GetRequestQuery(query);

        // Act
        var result = await sut.ExtractContent(request);

        // Assert
        result.Should().NotBeNull();
        (result!.OperationType ?? "").Should().Be(operationType);
        (result!.OperationName ?? "").Should().Be(operationName);
        result!.Query.Should().Be(query);
    }

    [Fact]
    public async Task ExtractContent_WithNullRequest_ReturnsNull()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        var result = await sut.ExtractContent(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractContent_WithNullRequestContent_ReturnsNull()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var request = SentryGraphQlTestHelpers.GetRequest(null);

        // Act
        var result = await sut.ExtractContent(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractContent_WithUnreadableStream_ReturnsNull()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var stream = new MemoryStream();
        var content = new StreamContent(stream);
        var request = SentryGraphQlTestHelpers.GetRequest(content);

        // Act
        var result = await sut.ExtractContent(request);

        // Assert
        result.Should().BeNull();
    }
}
