using System.Text.RegularExpressions;

namespace Sentry.Tests;

public class TracePropagationTargetTests
{
    [Fact]
    public void Substring_Matches()
    {
        var target = new TracePropagationTarget("cde");
        var isMatch = target.IsMatch("abcdef");
        Assert.True(isMatch);
    }

    [Fact]
    public void Substring_Doesnt_Match()
    {
        var target = new TracePropagationTarget("xyz");
        var isMatch = target.IsMatch("abcdef");
        Assert.False(isMatch);
    }

    [Fact]
    public void Substring_Matches_CaseInsensitive_ByDefault()
    {
        var target = new TracePropagationTarget("cDe");
        var isMatch = target.IsMatch("ABCdEF");
        Assert.True(isMatch);
    }

    [Fact]
    public void Substring_Matches_CaseSensitive()
    {
        var target = new TracePropagationTarget("CdE", StringComparison.Ordinal);
        var isMatch = target.IsMatch("ABCdEF");
        Assert.True(isMatch);
    }

    [Fact]
    public void Substring_Doesnt_Match_WhenCaseSensitive()
    {
        var target = new TracePropagationTarget("cDe", StringComparison.Ordinal);
        var isMatch = target.IsMatch("ABCdEF");
        Assert.False(isMatch);
    }

    [Fact]
    public void Regex_Object_Matches()
    {
        var regex = new Regex("^abc.*ghi$");
        var target = new TracePropagationTarget(regex);
        var isMatch = target.IsMatch("abcdefghi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Object_Doesnt_Match()
    {
        var regex = new Regex("^abc.*ghi$");
        var target = new TracePropagationTarget(regex);
        var isMatch = target.IsMatch("abcdef");
        Assert.False(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Matches()
    {
        var target = new TracePropagationTarget("^abc.*ghi$");
        var isMatch = target.IsMatch("abcdefghi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Matches_CaseInsensitive_ByDefault()
    {
        var target = new TracePropagationTarget("^abc.*ghi$");
        var isMatch = target.IsMatch("aBcDeFgHi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Matches_CaseSensitive()
    {
        var target = new TracePropagationTarget("^aBc.*gHi$", StringComparison.Ordinal);
        var isMatch = target.IsMatch("aBcDeFgHi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Doesnt_Match_WhenCaseSensitive()
    {
        var target = new TracePropagationTarget("^abc.*ghi$", StringComparison.Ordinal);
        var isMatch = target.IsMatch("aBcDeFgHi");
        Assert.False(isMatch);
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_DefaultAll()
    {
        var options = new SentryOptions();
        Assert.Equal(1, options.TracePropagationTargets.Count);
        Assert.Equal(".*", options.TracePropagationTargets[0].ToString());
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_AddRemovesDefault()
    {
        var options = new SentryOptions();
        options.TracePropagationTargets.Add(new TracePropagationTarget("foo"));
        options.TracePropagationTargets.Add(new TracePropagationTarget("bar"));

        Assert.Equal(2, options.TracePropagationTargets.Count);
        Assert.Equal("foo", options.TracePropagationTargets[0].ToString());
        Assert.Equal("bar", options.TracePropagationTargets[1].ToString());
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_SetRemovesDefault()
    {
        var options = new SentryOptions();
        var targets = new []
        {
            new TracePropagationTarget(".*"),
            new TracePropagationTarget("foo"),
            new TracePropagationTarget("bar")
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

        var result1 = options.TracePropagationTargets.ShouldPropagateTrace("foo");
        var result2 = options.TracePropagationTargets.ShouldPropagateTrace("");
        var result3 = options.TracePropagationTargets.ShouldPropagateTrace(null!);

        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_EmptyPropagatesNone()
    {
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<TracePropagationTarget>()
        };

        var result1 = options.TracePropagationTargets.ShouldPropagateTrace("foo");
        var result2 = options.TracePropagationTargets.ShouldPropagateTrace("");
        var result3 = options.TracePropagationTargets.ShouldPropagateTrace(null!);

        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_OneMatchPropagates()
    {
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<TracePropagationTarget>
            {
                new("foo"),
                new("localhost"),
                new("bar")
            }
        };

        var result = options.TracePropagationTargets.ShouldPropagateTrace("http://localhost/abc/123");
        Assert.True(result);
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_MultipleMatchesPropagates()
    {
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<TracePropagationTarget>
            {
                new("foo"),
                new("localhost"),
                new("bar")
            }
        };

        var result = options.TracePropagationTargets.ShouldPropagateTrace("http://localhost/foo/123");
        Assert.True(result);
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_NoMatchesDoesntPropagates()
    {
        var options = new SentryOptions
        {
            TracePropagationTargets = new List<TracePropagationTarget>
            {
                new("foo"),
                new("localhost"),
                new("bar")
            }
        };

        var result = options.TracePropagationTargets.ShouldPropagateTrace("https://sentry.io/abc/123");
        Assert.False(result);
    }
}
