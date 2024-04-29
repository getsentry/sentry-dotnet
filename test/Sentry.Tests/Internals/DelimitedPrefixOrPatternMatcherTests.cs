namespace Sentry.Tests.Internals;

public class DelimitedPrefixOrPatternMatcherTests
{
    private readonly DelimitedPrefixOrPatternMatcher _default = new();
    private readonly DelimitedPrefixOrPatternMatcher _caseSensitive = new('.', StringComparison.Ordinal);

    [Fact]
    public void IsMatch_DoesNotMatchSubstrings()
    {
        var target = new StringOrRegex("bc");
        var isMatch = _default.IsMatch(target, "abc.com");
        isMatch.Should().BeFalse();
    }

    [Theory]
    [InlineData("ab", true)]
    [InlineData("AB", true)]
    public void IsMatch_CaseInsensitive_MatchesPrefixes(string prefix, bool shouldMatch)
    {
        var target = new StringOrRegex(prefix);
        var isMatch = _default.IsMatch(target, "AB.com");
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("AB", true)]
    [InlineData("ab", false)]
    public void Prefix_CaseSensitive_MatchesSameCasePrefix(string prefix, bool shouldMatch)
    {
        var target = new StringOrRegex(prefix);
        var isMatch = _caseSensitive.IsMatch(target, "AB.com");
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData(@"^abc", true)]
    [InlineData(@"^abc.*", false)]
    public void Regex_IsMatch_RequiredSeparator(string pattern, bool shouldMatch)
    {
        var regex = new Regex(pattern);
        var target = new StringOrRegex(regex);
        var isMatch = _default.IsMatch(target, "abc.com");
        Assert.Equal(shouldMatch, isMatch);
    }
}
