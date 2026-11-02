namespace Sentry.Tests.Internals;

public class MonitorSlugTests
{
    // Expected values come from Relay's slugify_monitor_slug tests
    [Theory]
    [InlineData("my-monitor", "my-monitor")]
    [InlineData("My Monitor", "my-monitor")]
    [InlineData("my_job", "my_job")]
    [InlineData("  leading and trailing  ", "leading-and-trailing")]
    [InlineData("a - ! - b", "a-b")]
    [InlineData("--a--b--", "a-b")]
    [InlineData("_-_a_-_", "a")]
    [InlineData("a\tb\nc", "a-b-c")]
    [InlineData("a\u001cb", "a-b")]
    [InlineData("Café Crème", "cafe-creme")]
    [InlineData("straße", "strae")]
    [InlineData("ﬁle", "file")]
    [InlineData("Hello, World!", "hello-world")]
    [InlineData("a.b.c", "abc")]
    [InlineData("ÀÉÎÕÜ", "aeiou")]
    [InlineData("日本語", null)]
    [InlineData("!!!", null)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-b", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa_b", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa_")]
    public void From_Value_MatchesRelay(string value, string expected)
    {
        MonitorSlug.From(value).Should().Be(expected);
    }
}
