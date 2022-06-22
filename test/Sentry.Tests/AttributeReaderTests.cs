using AttributeReader = Sentry.AttributeReader;

public class AttributeReaderTests
{
    [Fact]
    public void Simple()
    {
        var assembly = typeof(AttributeReaderTests).Assembly;
        Assert.True(AttributeReader.TryGetProjectDirectory(assembly, out var projectDirectory));
        Assert.NotNull(projectDirectory);
        Assert.True(AttributeReader.TryGetSolutionDirectory(assembly, out var solutionDirectory));
        Assert.NotNull(solutionDirectory);
    }
}
