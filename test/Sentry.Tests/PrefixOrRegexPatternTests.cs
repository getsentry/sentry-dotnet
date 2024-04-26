namespace Sentry.Tests;

public class PrefixOrRegexPatternTests
{
    [Theory]
    [InlineData("ab", true)]
    [InlineData("bc", false)]
    public void Prefix_Matches(string prefix, bool shouldMatch)
    {
        var target = new PrefixOrRegexPattern(prefix);
        var isMatch = target.IsMatch("ABCdEF");
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("AB", true)]
    [InlineData("ab", false)]
    public void Prefix_CaseSensitive_Matches(string prefix, bool shouldMatch)
    {
        var target = new PrefixOrRegexPattern(prefix, StringComparison.Ordinal);
        var isMatch = target.IsMatch("ABCdEF");
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("ab", false)]
    [InlineData("abc", true)]
    [InlineData("AbC", true)]
    public void Prefix_RequiredSeparator_Matches(string prefix, bool shouldMatch)
    {
        var target = new PrefixOrRegexPattern(prefix);
        var isMatch = target.IsMatch("abc.def", '.');
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("^abc.*ghi$", true)]
    [InlineData("^abc.*gh$", false)]
    public void Regex_Matches(string pattern, bool shouldMatch)
    {
        var regex = new Regex(pattern);
        var target = new PrefixOrRegexPattern(regex);
        var isMatch = target.IsMatch("abcdefghi");
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData(@"^abc", true)]
    [InlineData(@"^abc.*", false)]
    public void Regex_RequiredSeparator_Matches(string pattern, bool shouldMatch)
    {
        var regex = new Regex(pattern);
        var target = new PrefixOrRegexPattern(regex);
        var isMatch = target.IsMatch("abc.com", '.');
        Assert.Equal(shouldMatch, isMatch);
    }

    [Fact]
    public void PrefixOrRegexPattern_ImplicitlyConvertsFromString()
    {
        PrefixOrRegexPattern target = "abc";
        var isMatch = target.IsMatch("abcdefghi");
        Assert.True(isMatch);
    }

    [Fact]
    public void PrefixOrRegexPattern_ImplicitlyConvertsFromRegex()
    {
        PrefixOrRegexPattern target = new Regex("^abc.*ghi$");
        var isMatch = target.IsMatch("abcdefghi");
        Assert.True(isMatch);
    }
}
