using Sentry.Internal.Http;

namespace Sentry.GraphQL.Client.Tests;

public class GraphQlRequestContentExtractorTests
{
    private const string ValidQuery = "query { notes { id } }";
    private const string ValidQueryWithName = "query getAllNotes { notes { id } }";
    private const string ValidShorthandQuery = "{ notes { id } }";
    private const string ValidMutation = "mutation saveSomething { id }";

    [Theory]
    [InlineData(ValidQuery, "query", "")]
    [InlineData(ValidShorthandQuery, "query", "")]
    [InlineData(ValidQueryWithName, "query", "getAllNotes")]
    [InlineData(ValidMutation, "mutation", "saveSomething")]
    public async Task ExtractContent_ValidQuery_UnpacksRequest(string query, string operationType, string operationName)
    {
        // Arrange
        var request = SentryGraphQlTestHelpers.GetRequestQuery(query);

        // Act
        var result = await GraphQLContentExtractor.ExtractRequestContentAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        (result!.OperationType ?? "").Should().Be(operationType);
        (result!.OperationName ?? "").Should().Be(operationName);
        result!.Query.Should().Be(query);
    }

    [Fact]
    public async Task ExtractContent_WithNullRequest_ReturnsNull()
    {
        // Act
        var result = await GraphQLContentExtractor.ExtractRequestContentAsync(null!, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractContent_WithNullRequestContent_ReturnsNull()
    {
        // Arrange
        var request = SentryGraphQlTestHelpers.GetRequest(null);

        // Act
        var result = await GraphQLContentExtractor.ExtractRequestContentAsync(request, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractContent_WithUnreadableStream_ReturnsNull()
    {
        // Arrange
        var stream = new MemoryStream();
        stream.Close();
        var content = new StreamContent(stream);
        var request = SentryGraphQlTestHelpers.GetRequest(content);

        // Act
        var result = await GraphQLContentExtractor.ExtractRequestContentAsync(request, null);

        // Assert
        result.Should().BeNull();
    }
}
