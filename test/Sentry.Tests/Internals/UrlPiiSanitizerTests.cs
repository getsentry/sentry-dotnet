namespace Sentry.Tests.Internals;

public class UrlPiiSanitizerTests
{
    [Fact]
    public void Sanitize_Null()
    {
        var actual = UrlPiiSanitizer.Sanitize(null);

        Assert.Null(actual);
    }

    [Theory]
    [InlineData("I'm a harmless string.", "doesn't affect ordinary strings")]
    [InlineData("htps://user:password@sentry.io?q=1&s=2&token=secret#top", "doesn't affect malformed https urls")]
    [InlineData("htp://user:password@sentry.io?q=1&s=2&token=secret#top", "doesn't affect malformed http urls")]
    public void Sanitize_Data_IsNotNull_WithoutPii(string original, string reason)
    {
        var actual = UrlPiiSanitizer.Sanitize(original);

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
    public void Sanitize_Data_IsNotNull_WithPii(string original, string expected, string reason)
    {
        var actual = UrlPiiSanitizer.Sanitize(original);

        actual.Should().Be(expected, reason);
    }
}
