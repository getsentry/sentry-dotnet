﻿namespace Sentry.Tests;

public class AttributeReaderTests
{
    [Fact]
    public void Simple()
    {
        var assembly = typeof(AttributeReaderTests).Assembly;
        Assert.True(AttributeReader.TryGetProjectDirectory(assembly, out var projectDirectory));
        Assert.NotNull(projectDirectory);
    }
}
