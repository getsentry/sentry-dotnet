namespace Sentry.Tests;

public class SubstringOrPatternMatcherTests
{
    private static class Fixture
    {
        public static SubstringOrPatternMatcher GetSut() => new();
        public static SubstringOrPatternMatcher GetSut(StringComparison comparison) => new(comparison);
    }

    [Theory]
    [InlineData("cde", "abcdef", true)]
    [InlineData("cDe", "ABCdEF", true)]
    [InlineData("xyz", "abcdef", false)]
    public void Substring_Matches(string substring, string testString, bool expected)
    {
        // Arrange
        var sut = Fixture.GetSut();

        // Act
        var isMatch = sut.IsMatch(substring, testString);

        // Assert
        isMatch.Should().Be(expected);
    }

    [Theory]
    [InlineData("CdE", true)]
    [InlineData("cDe", false)]
    public void Substring_Matches_CaseSensitive(string testString, bool expected)
    {
        // Arrange
        var sut = Fixture.GetSut(StringComparison.Ordinal);

        // Act
        var isMatch = sut.IsMatch(testString, "ABCdEF");

        // Assert
        isMatch.Should().Be(expected);
    }

    [Theory]
    [InlineData("^abc.*ghi$", "abcdefghi", true)]
    [InlineData("^abc.*ghi$", "aBcDeFgHi", true)] // Case insensitive
    [InlineData("^abc.*ghi$", "abcdef", false)]
    public void Regex_Matches(string pattern, string testString, bool expected)
    {
        // Arrange
        var sut = Fixture.GetSut();
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        var stringOrRegex = new StringOrRegex(regex);

        // Act
        var isMatch = sut.IsMatch(stringOrRegex, testString);

        // Assert
        isMatch.Should().Be(expected);
    }

    [Theory]
    [InlineData("^aBc.*gHi$", "aBcDeFgHi", true)]
    [InlineData("^abc.*ghi$", "aBcDeFgHi", false)]
    public void Regex_Matches_CaseSensitive(string pattern, string testString, bool expected)
    {
        // Arrange
        var sut = Fixture.GetSut();
        var regex = new Regex(pattern, RegexOptions.None);
        var stringOrRegex = new StringOrRegex(regex);

        // Act
        var isMatch = sut.IsMatch(stringOrRegex, testString);

        // Assert
        isMatch.Should().Be(expected);
    }
}
