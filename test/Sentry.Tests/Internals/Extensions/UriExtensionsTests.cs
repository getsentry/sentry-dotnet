namespace Sentry.Tests.Internals.Extensions;

#if NET6_0_OR_GREATER
public class UriExtensionsTests
{
    [Fact]
    public void FilteredHttpRequestUrl_ReturnsHttpRequestUrl()
    {
        // Arrange
        var baseUri = new Uri("https://www.contoso.com");
        var uri = new Uri(baseUri, "catalog/shownew.htm?date=today");
        var expected = "https://www.contoso.com/catalog/shownew.htm?date=today";

        // Act
        var actual = uri.FilteredHttpRequestUrl();

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void FilteredHttpRequestUrl_SensitiveParameter_FiltersValueAndPreservesEncoding()
    {
        var uri = new Uri("https://user:pass@example.com/api/users?token=abc123&q=a%20b%26c&page=5#top");

        var actual = uri.FilteredHttpRequestUrl();

        actual.Should().Be("https://example.com/api/users?token=[Filtered]&q=a%20b%26c&page=5");
    }

    [Fact]
    public void FilteredHttpRequestUrl_UnparseableQuery_KeepsQueryMarker()
    {
        var uri = new Uri("https://example.com/api?page=5&=abc");

        var actual = uri.FilteredHttpRequestUrl();

        actual.Should().Be("https://example.com/api?[Filtered]");
    }

    [Fact]
    public void FilteredHttpRequestUrl_DangerousDisablePathAndQueryCanonicalization_ReturnsHttpRequestUrl()
    {
        // Arrange
        var options = new UriCreationOptions();
        options.DangerousDisablePathAndQueryCanonicalization = true;
        var uri = new Uri("https://www.contoso.com", options);
        var expected = "https://www.contoso.com/";

        // Act
        var actual = uri.FilteredHttpRequestUrl();

        // Assert
        actual.Should().Be(expected);
    }
}
#endif
