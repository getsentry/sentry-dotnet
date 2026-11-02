namespace Sentry.Tests;

public class StringOrRegexTests
{
    [Fact]
    public void Constructor_String_IsRegexFalse()
    {
        var target = new StringOrRegex("abc");
        target.IsRegex.Should().BeFalse();
    }

    [Fact]
    public void Constructor_Regex_IsRegexTrue()
    {
        var target = new StringOrRegex(new Regex("^abc.*ghi$"));
        target.IsRegex.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullRegex_IsRegexFalse()
    {
        var target = new StringOrRegex((Regex)null!);
        target.IsRegex.Should().BeFalse();
        target.ToString().Should().BeEmpty();
    }

    [Fact]
    public void ImplicitConversion_String_PreservesValueAndType()
    {
        StringOrRegex target = "abc";
        target.IsRegex.Should().BeFalse();
        target._string.Should().Be("abc");
        target._regex.Should().BeNull();
    }

    [Fact]
    public void ImplicitConversion_Regex_PreservesValueAndType()
    {
        StringOrRegex target = new Regex("^abc.*ghi$");
        target.IsRegex.Should().BeTrue();
        target._string.Should().BeNull();
        target._regex.Should().NotBeNull();
        target._regex?.ToString().Should().Be("^abc.*ghi$");
    }
}
