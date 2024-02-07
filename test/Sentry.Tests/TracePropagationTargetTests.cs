namespace Sentry.Tests;

public class TracePropagationTargetTests
{
    [Fact]
    public void SentryOptions_TracePropagationTargets_DefaultAll()
    {
        var options = new SentryOptions();
        Assert.Single(options.TracePropagationTargets);
        Assert.Equal(".*", options.TracePropagationTargets[0].ToString());
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_AddRemovesDefault()
    {
        var options = new SentryOptions();
        options.TracePropagationTargets.Add(new SubstringOrRegexPattern("foo"));
        options.TracePropagationTargets.Add(new SubstringOrRegexPattern("bar"));

        Assert.Equal(2, options.TracePropagationTargets.Count);
        Assert.Equal("foo", options.TracePropagationTargets[0].ToString());
        Assert.Equal("bar", options.TracePropagationTargets[1].ToString());
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_SetRemovesDefault()
    {
        var options = new SentryOptions();
        var targets = new[]
        {
            new SubstringOrRegexPattern(".*"),
            new SubstringOrRegexPattern("foo"),
            new SubstringOrRegexPattern("bar")
        };

        options.TracePropagationTargets = targets;

        Assert.Equal(2, options.TracePropagationTargets.Count);
        Assert.Equal("foo", options.TracePropagationTargets[0].ToString());
        Assert.Equal("bar", options.TracePropagationTargets[1].ToString());
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_DefaultPropagatesAll()
    {
        var options = new SentryOptions();

        var result1 = options.TracePropagationTargets.ContainsMatch("foo");
        var result2 = options.TracePropagationTargets.ContainsMatch("");
        var result3 = options.TracePropagationTargets.ContainsMatch(null!);

        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_EmptyPropagatesNone()
    {
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<SubstringOrRegexPattern>()
        };

        var result1 = options.TracePropagationTargets.ContainsMatch("foo");
        var result2 = options.TracePropagationTargets.ContainsMatch("");
        var result3 = options.TracePropagationTargets.ContainsMatch(null!);

        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_OneMatchPropagates()
    {
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<SubstringOrRegexPattern>
            {
                new("foo"),
                new("localhost"),
                new("bar")
            }
        };

        var result = options.TracePropagationTargets.ContainsMatch("http://localhost/abc/123");
        Assert.True(result);
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_MultipleMatchesPropagates()
    {
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<SubstringOrRegexPattern>
            {
                new("foo"),
                new("localhost"),
                new("bar")
            }
        };

        var result = options.TracePropagationTargets.ContainsMatch("http://localhost/foo/123");
        Assert.True(result);
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_NoMatchesDoesntPropagates()
    {
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<SubstringOrRegexPattern>
            {
                new("foo"),
                new("localhost"),
                new("bar")
            }
        };

        var result = options.TracePropagationTargets.ContainsMatch("https://sentry.io/abc/123");
        Assert.False(result);
    }
}
