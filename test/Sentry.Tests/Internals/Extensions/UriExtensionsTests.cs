namespace Sentry.Tests.Internals.Extensions;

#if NET6_0_OR_GREATER
public class UriExtensionsTests
{
    [Fact]
    public void HttpRequestUrl_ReturnsHttpRequestUrl()
    {
        // Arrange
        var baseUri = new Uri("https://www.contoso.com");
        var uri = new Uri(baseUri, "catalog/shownew.htm?date=today");
        var expected = "https://www.contoso.com/catalog/shownew.htm?date=today";

        // Act
        var actual = uri.HttpRequestUrl();

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void HttpRequestUrl_DangerousDisablePathAndQueryCanonicalization_ReturnsHttpRequestUrl()
    {
        // Arrange
        var options = new UriCreationOptions();
        options.DangerousDisablePathAndQueryCanonicalization = true;
        var uri = new Uri("https://www.contoso.com", options);
        var expected = "https://www.contoso.com/";

        // Act
        var actual = uri.HttpRequestUrl();

        // Assert
        actual.Should().Be(expected);
    }
}
#endif
