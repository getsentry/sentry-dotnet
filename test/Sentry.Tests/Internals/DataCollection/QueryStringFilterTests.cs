using Sentry.Internal.DataCollection;

namespace Sentry.Tests.Internals.DataCollection;

public class QueryStringFilterTests
{
    [Theory]
    [InlineData("token=abc123&q=a%20b%26c&page=5", "token=[Filtered]&q=a%20b%26c&page=5")]
    [InlineData("?token=abc123&q=a%20b%26c&page=5", "?token=[Filtered]&q=a%20b%26c&page=5")]
    [InlineData("?access_token=abc", "?access_token=[Filtered]")]
    [InlineData("SESSION_ID=abc&Api-Key=def", "SESSION_ID=[Filtered]&Api-Key=[Filtered]")]
    [InlineData("%74oken=abc", "%74oken=[Filtered]")]
    [InlineData("token=a=b=c&page=5", "token=[Filtered]&page=5")]
    [InlineData("token=&q=", "token=[Filtered]&q=")]
    public void Filter_SensitiveKey_ReplacesOnlyItsValue(string input, string expected) =>
        QueryStringFilter.Filter(input).Should().Be(expected);

    [Theory]
    [InlineData("q=a%20b%26c&page=5")]
    [InlineData("q=a%26token=secret")]
    [InlineData("q=a+b&redirect=https%3A%2F%2Fexample.com%2F%3Fa%3D1")]
    [InlineData("q=%41%e4")]
    [InlineData("q=100%")]
    [InlineData("q=&page=")]
    [InlineData("flag")]
    [InlineData("token")]
    [InlineData("flag&token&page=5")]
    [InlineData("a=1&&b=2&")]
    [InlineData("=")]
    [InlineData("?")]
    public void Filter_NothingToFilter_ReturnsInputUnchanged(string input) =>
        QueryStringFilter.Filter(input).Should().BeSameAs(input);

    [Theory]
    [InlineData("%zz=abc")]
    [InlineData("to%ken=abc")]
    [InlineData("%74oken%=abc")]
    [InlineData("%74oken%7=abc")]
    [InlineData("%u0074oken=abc")]
    [InlineData("=abc")]
    [InlineData("?page=5&=abc")]
    public void Filter_UnparseableName_FiltersWholeQueryString(string input) =>
        QueryStringFilter.Filter(input).Should().Be(SensitiveDataFilter.FilteredValue);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Filter_NoQueryString_ReturnsInput(string input) =>
        QueryStringFilter.Filter(input).Should().Be(input);

    [Theory]
    [InlineData(
        "https://example.com/api/users?token=abc123&q=a%20b%26c&page=5",
        "https://example.com/api/users?token=[Filtered]&q=a%20b%26c&page=5")]
    [InlineData("https://example.com/?token=abc#token=frag", "https://example.com/?token=[Filtered]#token=frag")]
    [InlineData("/api/users?token=abc", "/api/users?token=[Filtered]")]
    [InlineData("https://example.com/?%zz=abc#top", "https://example.com/?[Filtered]#top")]
    public void FilterUrl_UrlWithQuery_FiltersQuery(string input, string expected) =>
        QueryStringFilter.FilterUrl(input).Should().Be(expected);

    [Theory]
    [InlineData("https://example.com/api/users")]
    [InlineData("https://example.com/api/users?q=a%20b&page=5#top")]
    [InlineData("https://example.com/#a?token=abc")]
    [InlineData("")]
    [InlineData(null)]
    public void FilterUrl_NothingToFilter_ReturnsInputUnchanged(string input) =>
        QueryStringFilter.FilterUrl(input).Should().BeSameAs(input);

    [Theory]
    [InlineData("https://example.com/api/users?token=abc&q=1", "https://example.com/api/users")]
    [InlineData("https://example.com/api/users#top", "https://example.com/api/users")]
    [InlineData("https://example.com/api/users?a=1#top", "https://example.com/api/users")]
    [InlineData("https://example.com/api/users", "https://example.com/api/users")]
    [InlineData("/api/users?a=1", "/api/users")]
    public void RemoveQueryAndFragment_ReturnsUrlWithoutThem(string input, string expected) =>
        QueryStringFilter.RemoveQueryAndFragment(input).Should().Be(expected);
}
