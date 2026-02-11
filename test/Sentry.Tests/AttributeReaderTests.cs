namespace Sentry.Tests;

public class AttributeReaderTests
{
    [Fact]
    public void Simple()
    {
        var assembly = typeof(AttributeReaderTests).Assembly;
#pragma warning disable IDE0055 // Disable formatting - see https://github.com/getsentry/sentry-dotnet/pull/4911#discussion_r2795717887
        Assert.NotNull(AttributeReader.TryGetProjectDirectory(assembly));
#pragma warning restore IDE0055 // Restore formatting        
    }
}
