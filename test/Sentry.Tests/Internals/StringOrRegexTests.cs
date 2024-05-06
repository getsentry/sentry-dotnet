namespace Sentry.Tests.Internals;

public class StringOrRegexTests
{
    [Fact]
    public void StringOrRegex_ImplicitlyConvertsFromString()
    {
        StringOrRegex target = "abc";
        target._prefix.Should().Be("abc");
        target._regex.Should().BeNull();
    }

    [Fact]
    public void StringOrRegex_ImplicitlyConvertsFromRegex()
    {
        StringOrRegex target = new Regex("^abc.*ghi$");
        target._prefix.Should().BeNull();
        target._regex.Should().NotBeNull();
        target._regex?.ToString().Should().Be("^abc.*ghi$");
    }
}
