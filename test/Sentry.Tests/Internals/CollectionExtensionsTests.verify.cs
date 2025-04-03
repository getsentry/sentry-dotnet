namespace Sentry.Tests.Internals;

public class CollectionExtensionsTests
{
    [Fact]
    public Task GetOrCreate_invalid_type()
    {
        var dictionary = new ConcurrentDictionary<string, object> { ["key"] = 1 };
        return Throws(() => dictionary.GetOrCreate<Value>("key"))
            .IgnoreStackTrace();
    }

    private class Value
    {
    }

    [Fact]
    public void TryGetValue_KeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var dictionary = new ConcurrentDictionary<string, object>();

        // Act
        var result = dictionary.TryGetValue<string>("nonexistentKey");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryGetValue_KeyExistsAndTypeMatches_ReturnsValue()
    {
        // Arrange
        var dictionary = new ConcurrentDictionary<string, object>
        {
            ["existingKey"] = "testValue"
        };

        // Act
        var result = dictionary.TryGetValue<string>("existingKey");

        // Assert
        result.Should().Be("testValue");
    }

    [Fact]
    public void TryGetValue_KeyExistsButTypeDoesNotMatch_Throws()
    {
        // Arrange
        var dictionary = new ConcurrentDictionary<string, object>
        {
            ["existingKey"] = 123
        };

        // Act
        Action act = () => dictionary.TryGetValue<string>("existingKey");

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("Expected a type of System.String to exist for the key 'existingKey'. Instead found a System.Int32. The likely cause of this is that the value for 'existingKey' has been incorrectly set to an instance of a different type.");
    }
}
