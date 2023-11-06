namespace Sentry.Tests.Internals.Extensions;

public class ReadOnlyDictionaryExtensionsTests
{
    [Fact]
    public void TryGetValue_ShouldReturnNull_WhenKeyNotFound()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>(); // Empty dictionary

        // Act
        var result = DictionaryExtensions.TryGetValue<string, string>(dictionary,"key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryGetValue_ShouldReturnValue_WhenKeyFoundAndTypeMatches()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            { "key", "value" }
        };

        // Act
        var result = DictionaryExtensions.TryGetValue<string, string>(dictionary, "key");

        // Assert
        result.Should().Be("value");
    }

    [Fact]
    public void TryGetValue_ShouldReturnNull_WhenKeyFoundButTypeDoesNotMatch()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            { "key", 123 }
        };

        // Act
        var result = DictionaryExtensions.TryGetValue<string, string>(dictionary, "key");

        // Assert
        result.Should().BeNull();
    }
}
