namespace Sentry.Tests.Internals;

public class RedactedHeadersTests
{
    [Fact]
    public void Add_WithAuthorizationKey_ShouldStoreFilteredValue()
    {
        // Arrange
        var headers = new RedactedHeaders();

        // Act
        headers.Add("Authorization", "Bearer 123");

        // Assert
        headers["Authorization"].Should().Be("[Filtered]");
    }

    [Fact]
    public void IndexerSet_WithAuthorizationKey_ShouldStoreFilteredValue()
    {
        // Arrange
        var headers = new RedactedHeaders();

        // Act
        headers["Authorization"] = "Bearer 456";

        // Assert
        headers["Authorization"].Should().Be("[Filtered]");
    }

    [Fact]
    public void Add_WithOtherKey_ShouldStoreOriginalValue()
    {
        // Arrange
        var headers = new RedactedHeaders();

        // Act
        headers.Add("User-Agent", "TestAgent");

        // Assert
        headers["User-Agent"].Should().Be("TestAgent");
    }

    [Fact]
    public void IndexerGet_WithMissingKey_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var headers = new RedactedHeaders();

        // Act
        var act = () => _ = headers["Missing"];

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryGetValue_WithExistingKey_ShouldReturnTrueAndValue()
    {
        // Arrange
        var headers = new RedactedHeaders
        {
            ["Authorization"] = "secret"
        };

        // Act
        var success = headers.TryGetValue("Authorization", out var value);

        // Assert
        success.Should().BeTrue();
        value.Should().Be("[Filtered]");
    }

    [Fact]
    public void TryGetValue_WithMissingKey_ShouldReturnFalse()
    {
        // Arrange
        var headers = new RedactedHeaders();

        // Act
        var success = headers.TryGetValue("Nonexistent", out var value);

        // Assert
        success.Should().BeFalse();
        value.Should().BeNull(); // nullable context may allow this
    }

    [Fact]
    public void ImplicitConversion_FromDictionary_ShouldRedactAuthorization()
    {
        // Arrange
        var dict = new Dictionary<string, string>
        {
            { "Authorization", "Token xyz" },
            { "Custom", "Value" }
        };

        // Act
        RedactedHeaders headers = dict;

        // Assert
        headers["Authorization"].Should().Be("[Filtered]");
        headers["Custom"].Should().Be("Value");
    }

    [Fact]
    public void CaseInsensitiveKeyMatching_ShouldRedactAuthorization()
    {
        // Arrange
        var headers = new RedactedHeaders();

        // Act
        headers.Add("authorization", "should be filtered");

        // Assert
        headers["AUTHORIZATION"].Should().Be("[Filtered]");
    }
}
