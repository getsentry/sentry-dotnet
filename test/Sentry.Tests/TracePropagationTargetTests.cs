using System.Text.RegularExpressions;

namespace Sentry.Tests;

public class TracePropagationTargetTests
{
    [Fact]
    public void Substring_Matches()
    {
        var target = TracePropagationTarget.CreateFromSubstring("cde");
        var isMatch = target.IsMatch("abcdef");
        Assert.True(isMatch);
    }

    [Fact]
    public void Substring_Doesnt_Match()
    {
        var target = TracePropagationTarget.CreateFromSubstring("xyz");
        var isMatch = target.IsMatch("abcdef");
        Assert.False(isMatch);
    }

    [Fact]
    public void Substring_Matches_CaseInsensitive_ByDefault()
    {
        var target = TracePropagationTarget.CreateFromSubstring("cDe");
        var isMatch = target.IsMatch("ABCdEF");
        Assert.True(isMatch);
    }

    [Fact]
    public void Substring_Matches_CaseSensitive()
    {
        var target = TracePropagationTarget.CreateFromSubstring("CdE", caseSensitive: true);
        var isMatch = target.IsMatch("ABCdEF");
        Assert.True(isMatch);
    }

    [Fact]
    public void Substring_Doesnt_Match_WhenCaseSensitive()
    {
        var target = TracePropagationTarget.CreateFromSubstring("cDe", caseSensitive: true);
        var isMatch = target.IsMatch("ABCdEF");
        Assert.False(isMatch);
    }

    [Fact]
    public void Regex_Object_Matches()
    {
        var regex = new Regex("^abc.*ghi$");
        var target = TracePropagationTarget.CreateFromRegex(regex);
        var isMatch = target.IsMatch("abcdefghi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Object_Doesnt_Match()
    {
        var regex = new Regex("^abc.*ghi$");
        var target = TracePropagationTarget.CreateFromRegex(regex);
        var isMatch = target.IsMatch("abcdef");
        Assert.False(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Matches()
    {
        var target = TracePropagationTarget.CreateFromRegex("^abc.*ghi$");
        var isMatch = target.IsMatch("abcdefghi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Matches_CaseInsensitive_ByDefault()
    {
        var target = TracePropagationTarget.CreateFromRegex("^abc.*ghi$");
        var isMatch = target.IsMatch("aBcDeFgHi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Matches_CaseSensitive()
    {
        var target = TracePropagationTarget.CreateFromRegex("^aBc.*gHi$", caseSensitive: true);
        var isMatch = target.IsMatch("aBcDeFgHi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Doesnt_Match_WhenCaseSensitive()
    {
        var target = TracePropagationTarget.CreateFromRegex("^abc.*ghi$", caseSensitive: true);
        var isMatch = target.IsMatch("aBcDeFgHi");
        Assert.False(isMatch);
    }

    [Fact]
    public void SentryOptions_TracePropagationTargets_DefaultNull()
    {
        var options = new SentryOptions();
        Assert.Null(options.TracePropagationTargets);
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
                TracePropagationTarget.CreateFromSubstring("foo"),
                TracePropagationTarget.CreateFromSubstring("localhost"),
                TracePropagationTarget.CreateFromSubstring("bar")
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
                TracePropagationTarget.CreateFromSubstring("foo"),
                TracePropagationTarget.CreateFromSubstring("localhost"),
                TracePropagationTarget.CreateFromSubstring("bar")
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
                TracePropagationTarget.CreateFromSubstring("foo"),
                TracePropagationTarget.CreateFromSubstring("localhost"),
                TracePropagationTarget.CreateFromSubstring("bar")
            }
        };

        var result = options.TracePropagationTargets.ShouldPropagateTrace("https://sentry.io/abc/123");
        Assert.False(result);
    }
}
