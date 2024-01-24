namespace Sentry.Tests;

public class FailedRequestTargetsTests
{
    [Fact]
    public void SentryOptions_FailedRequestTargets_DefaultAll()
    {
        var options = new SentryOptions();
        Assert.Single(options.FailedRequestTargets);
        Assert.Equal(".*", options.FailedRequestTargets[0].ToString());
    }

    [Fact]
    public void SentryOptions_FailedRequestTargets_AddRemovesDefault()
    {
        var options = new SentryOptions();
        options.FailedRequestTargets.Add("foo");
        options.FailedRequestTargets.Add("bar");

        Assert.Equal(2, options.FailedRequestTargets.Count);
        Assert.Equal("foo", options.FailedRequestTargets[0].ToString());
        Assert.Equal("bar", options.FailedRequestTargets[1].ToString());
    }

    [Fact]
    public void SentryOptions_FailedRequestTargets_SetRemovesDefault()
    {
        var options = new SentryOptions();
        var targets = new List<SubstringOrRegexPattern>
        {
            ".*",
            "foo",
            "bar"
        };

        options.FailedRequestTargets = targets;

        Assert.Equal(2, options.FailedRequestTargets.Count);
        Assert.Equal("foo", options.FailedRequestTargets[0].ToString());
        Assert.Equal("bar", options.FailedRequestTargets[1].ToString());
    }

    [Fact]
    public void SentryOptions_FailedRequestTargets_DefaultMatchesAll()
    {
        var options = new SentryOptions();

        var result1 = options.FailedRequestTargets.ContainsMatch("foo");
        var result2 = options.FailedRequestTargets.ContainsMatch("");
        var result3 = options.FailedRequestTargets.ContainsMatch(null!);

        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
    }

    [Fact]
    public void SentryOptions_FailedRequestTargets_EmptyMatchesNone()
    {
        var options = new SentryOptions
        {
            FailedRequestTargets = new List<SubstringOrRegexPattern>()
        };

        var result1 = options.FailedRequestTargets.ContainsMatch("foo");
        var result2 = options.FailedRequestTargets.ContainsMatch("");
        var result3 = options.FailedRequestTargets.ContainsMatch(null!);

        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public void SentryOptions_FailedRequestTargets_OneMatch()
    {
        var options = new SentryOptions
        {
            FailedRequestTargets = new List<SubstringOrRegexPattern>
            {
                "foo",
                "localhost",
                "bar"
            }
        };

        var result = options.FailedRequestTargets.ContainsMatch("http://localhost/abc/123");
        Assert.True(result);
    }

    [Fact]
    public void SentryOptions_FailedRequestTargets_MultipleMatches()
    {
        var options = new SentryOptions
        {
            FailedRequestTargets = new List<SubstringOrRegexPattern>
            {
                "foo",
                "localhost",
                "bar"
            }
        };

        var result = options.FailedRequestTargets.ContainsMatch("http://localhost/foo/123");
        Assert.True(result);
    }

    [Fact]
    public void SentryOptions_FailedRequestTargets_NoMatches()
    {
        var options = new SentryOptions
        {
            FailedRequestTargets = new List<SubstringOrRegexPattern>
            {
                "foo",
                "localhost",
                "bar"
            }
        };

        var result = options.FailedRequestTargets.ContainsMatch("https://sentry.io/abc/123");
        Assert.False(result);
    }
}
