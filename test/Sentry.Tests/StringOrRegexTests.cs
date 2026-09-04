namespace Sentry.Tests;

public class StringOrRegexTests
{
    [Fact]
    public void Constructor_String_TypeIsString()
    {
        var target = new StringOrRegex("abc");
        target.Type.Should().Be(StringOrRegexType.String);
    }

    [Fact]
    public void Constructor_Regex_TypeIsRegex()
    {
        var target = new StringOrRegex(new Regex("^abc.*ghi$"));
        target.Type.Should().Be(StringOrRegexType.Regex);
    }

    [Fact]
    public void ImplicitConversion_String_PreservesValueAndType()
    {
        StringOrRegex target = "abc";
        target.Type.Should().Be(StringOrRegexType.String);
        target._string.Should().Be("abc");
        target._regex.Should().BeNull();
    }

    [Fact]
    public void ImplicitConversion_Regex_PreservesValueAndType()
    {
        StringOrRegex target = new Regex("^abc.*ghi$");
        target.Type.Should().Be(StringOrRegexType.Regex);
        target._string.Should().BeNull();
        target._regex.Should().NotBeNull();
        target._regex?.ToString().Should().Be("^abc.*ghi$");
    }
}
