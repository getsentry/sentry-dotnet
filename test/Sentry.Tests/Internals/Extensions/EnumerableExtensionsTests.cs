namespace Sentry.Tests.Internals.Extensions;

public class EnumerableExtensionsTests
{
    [Fact]
    public void WithValues_WithNullValue_DoesNotReturn()
    {
        var items = new Dictionary<string, string>
        {
            ["key"] = null
        };

        var result = items.WithValues();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WithValues_WithNonNullValue_Returns()
    {
        var items = new Dictionary<string, string>
        {
            ["key"] = "value"
        };

        var result = items.WithValues();

        result.Should().NotBeEmpty();
    }
}
