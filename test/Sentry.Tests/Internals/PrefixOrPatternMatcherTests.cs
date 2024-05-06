namespace Sentry.Tests.Internals;

public class PrefixOrPatternMatcherTests
{
    private readonly PrefixOrPatternMatcher _default = new();
    private readonly PrefixOrPatternMatcher _caseSensitive = new(StringComparison.Ordinal);

    [Fact]
    public void IsMatch_DoesNotMatchSubstrings()
    {
        var target = new StringOrRegex("bc");
        var isMatch = _default.IsMatch(target, "ABCdEF");
        isMatch.Should().BeFalse();
    }

    [Theory]
    [InlineData("ab", true)]
    [InlineData("AB", true)]
    public void IsMatch_Default_MatchesAnyCase(string prefix, bool shouldMatch)
    {
        var target = new StringOrRegex(prefix);
        var isMatch = _default.IsMatch(target, "ABCdEF");
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("AB", true)]
    [InlineData("ab", false)]
    public void Prefix_CaseSensitive_MatchesSameCase(string prefix, bool shouldMatch)
    {
        var target = new StringOrRegex(prefix);
        var isMatch = _caseSensitive.IsMatch(target, "ABCdEF");
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("^abc.*ghi$", true)]
    [InlineData("^abc.*gh$", false)]
    public void Regex_Default_MatchesPattern(string pattern, bool shouldMatch)
    {
        var regex = new Regex(pattern);
        var target = new StringOrRegex(regex);
        var isMatch = _default.IsMatch(target, "abcdefghi");
        Assert.Equal(shouldMatch, isMatch);
    }
}
