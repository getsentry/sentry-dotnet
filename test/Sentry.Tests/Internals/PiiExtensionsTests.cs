namespace Sentry.Tests.Internals;

public class PiiExtensionsTests
{
    [Fact]
    public void RedactUrl_Null()
    {
        var actual = PiiExtensions.RedactUrl(null);

        Assert.Null(actual);
    }

    [Theory]
    [InlineData("I'm a harmless string.", "doesn't affect ordinary strings")]
    [InlineData("htps://user:password@sentry.io?q=1&s=2&token=secret#top", "doesn't affect malformed https urls")]
    [InlineData("htp://user:password@sentry.io?q=1&s=2&token=secret#top", "doesn't affect malformed http urls")]
    [InlineData("GET https://api.example.com/users failed for bob@example.com", "doesn't treat a later '@' as userinfo")]
    [InlineData("https://example.com/users/@me", "doesn't treat an '@' in the path as userinfo")]
    [InlineData("https://example.com/search?q=a@b.com", "doesn't treat an '@' in the query as userinfo")]
    [InlineData("https://example.com/#a@b", "doesn't treat an '@' in the fragment as userinfo")]
    public void RedactUrl_NotNull_WithoutPii(string original, string reason)
    {
        var actual = original.RedactUrl();

        actual.Should().Be(original, reason);
    }

    [Theory]
    [InlineData("https://user:password@sentry.io?q=1&s=2&token=secret#top", "https://[Filtered]:[Filtered]@sentry.io?q=1&s=2&token=secret#top", "strips user info with user and password from https")]
    [InlineData("https://user:password@sentry.io", "https://[Filtered]:[Filtered]@sentry.io", "strips user info with user and password from https without query")]
    [InlineData("https://user@sentry.io", "https://[Filtered]@sentry.io", "strips user info with user only from https without query")]
    [InlineData("http://user:password@sentry.io?q=1&s=2&token=secret#top", "http://[Filtered]:[Filtered]@sentry.io?q=1&s=2&token=secret#top", "strips user info with user and password from http")]
    [InlineData("http://user:password@sentry.io", "http://[Filtered]:[Filtered]@sentry.io", "strips user info with user and password from http without query")]
    [InlineData("http://user@sentry.io", "http://[Filtered]@sentry.io", "strips user info with user only from http without query")]
    [InlineData("GET https://user@sentry.io for goodness", "GET https://[Filtered]@sentry.io for goodness", "strips user info from URL embedded in text")]
    [InlineData("https://user:p@ss@sentry.io/a@b", "https://[Filtered]:[Filtered]@sentry.io/a@b", "strips user info up to the last '@' in the authority")]
    [InlineData("https://a@x.io and https://b:c@y.io", "https://[Filtered]@x.io and https://[Filtered]:[Filtered]@y.io", "strips user info from each URL")]
    public void RedactUrl_NotNull_WithPii(string original, string expected, string reason)
    {
        var actual = original.RedactUrl();

        actual.Should().Be(expected, reason);
    }
}
