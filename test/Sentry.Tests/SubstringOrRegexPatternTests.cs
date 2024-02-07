namespace Sentry.Tests;

public class SubstringOrRegexPatternTests
{
    [Fact]
    public void Substring_Matches()
    {
        var target = new SubstringOrRegexPattern("cde");
        var isMatch = target.IsMatch("abcdef");
        Assert.True(isMatch);
    }

    [Fact]
    public void Substring_Doesnt_Match()
    {
        var target = new SubstringOrRegexPattern("xyz");
        var isMatch = target.IsMatch("abcdef");
        Assert.False(isMatch);
    }

    [Fact]
    public void Substring_Matches_CaseInsensitive_ByDefault()
    {
        var target = new SubstringOrRegexPattern("cDe");
        var isMatch = target.IsMatch("ABCdEF");
        Assert.True(isMatch);
    }

    [Fact]
    public void Substring_Matches_CaseSensitive()
    {
        var target = new SubstringOrRegexPattern("CdE", StringComparison.Ordinal);
        var isMatch = target.IsMatch("ABCdEF");
        Assert.True(isMatch);
    }

    [Fact]
    public void Substring_Doesnt_Match_WhenCaseSensitive()
    {
        var target = new SubstringOrRegexPattern("cDe", StringComparison.Ordinal);
        var isMatch = target.IsMatch("ABCdEF");
        Assert.False(isMatch);
    }

    [Fact]
    public void Regex_Object_Matches()
    {
        var regex = new Regex("^abc.*ghi$");
        var target = new SubstringOrRegexPattern(regex);
        var isMatch = target.IsMatch("abcdefghi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Object_Doesnt_Match()
    {
        var regex = new Regex("^abc.*ghi$");
        var target = new SubstringOrRegexPattern(regex);
        var isMatch = target.IsMatch("abcdef");
        Assert.False(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Matches()
    {
        var target = new SubstringOrRegexPattern("^abc.*ghi$");
        var isMatch = target.IsMatch("abcdefghi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Matches_CaseInsensitive_ByDefault()
    {
        var target = new SubstringOrRegexPattern("^abc.*ghi$");
        var isMatch = target.IsMatch("aBcDeFgHi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Matches_CaseSensitive()
    {
        var target = new SubstringOrRegexPattern("^aBc.*gHi$", StringComparison.Ordinal);
        var isMatch = target.IsMatch("aBcDeFgHi");
        Assert.True(isMatch);
    }

    [Fact]
    public void Regex_Pattern_Doesnt_Match_WhenCaseSensitive()
    {
        var target = new SubstringOrRegexPattern("^abc.*ghi$", StringComparison.Ordinal);
        var isMatch = target.IsMatch("aBcDeFgHi");
        Assert.False(isMatch);
    }

    [Fact]
    public void SubstringOrRegexPattern_ImplicitlyConvertsFromString()
    {
        SubstringOrRegexPattern target = "^abc.*ghi$";
        var isMatch = target.IsMatch("abcdefghi");
        Assert.True(isMatch);
    }

    [Fact]
    public void SubstringOrRegexPattern_ImplicitlyConvertsFromRegex()
    {
        SubstringOrRegexPattern target = new Regex("^abc.*ghi$");
        var isMatch = target.IsMatch("abcdefghi");
        Assert.True(isMatch);
    }
}
