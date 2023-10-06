namespace Sentry.Tests;

public class AttributeReaderTests
{
    [Fact]
    public void Simple()
    {
        var assembly = typeof(AttributeReaderTests).Assembly;
        Assert.NotNull(AttributeReader.TryGetProjectDirectory(assembly));
    }
}
