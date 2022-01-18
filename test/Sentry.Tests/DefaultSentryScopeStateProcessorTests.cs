namespace Sentry.Tests;

public class DefaultSentryScopeStateProcessorTests
{
    [Theory]
    [InlineData("a", "a")]
    [InlineData("{}", "")]
    [InlineData("{OriginalFormat}", "OriginalFormat")]
    [InlineData("OriginalFormat", "OriginalFormat")]
    public void Apply_KeyValuePairObjectWithBraces_TagAddedWithoutBraces(string key, string expectedKey)
    {
        // Arrange
        var expectedValue = "some string";
        var list = new List<KeyValuePair<string, object>>
        {
            new(key, expectedValue)
        };
        var scopeStateProcessor = new DefaultSentryScopeStateProcessor();
        var scope = new Scope();

        // Act
        scopeStateProcessor.Apply(scope, list);

        // Assert
        Assert.Equal(expectedKey, scope.Tags.First().Key);
        Assert.Equal(expectedValue, scope.Tags.First().Value);
    }
}
