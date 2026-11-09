using Sentry.Internal.DataCollection;
using Trace = Sentry.Protocol.Trace;

namespace Sentry.Tests.Internals.DataCollection;

public class CookieFilterTests
{
    private const string Filtered = SensitiveDataFilter.FilteredValue;

    [Fact]
    public void FilterCookieHeader_IssueExample_FiltersEachCookie()
    {
        var result = CookieFilter.FilterCookieHeader("user_session=abc; theme=dark-mode;opaque-token");

        result.Should().Equal("user_session=[Filtered]", "theme=dark-mode", "[Filtered]");
    }

    [Fact]
    public void FilterSetCookieHeader_IssueExample_DropsAttributes()
    {
        var result = CookieFilter.FilterSetCookieHeader("theme=light-mode; HttpOnly");

        result.Should().Equal("theme=light-mode");
    }

    [Fact]
    public void FilterCookieHeader_NoSpaceAfterSemicolon_SplitsCookies()
    {
        var result = CookieFilter.FilterCookieHeader("theme=dark;user_session=abc;lang=en");

        result.Should().Equal("theme=dark", "user_session=[Filtered]", "lang=en");
    }

    [Theory]
    [InlineData("opaque-token")]
    [InlineData("=opaque-token")]
    public void FilterCookieHeader_NamelessCookie_IsFiltered(string header)
    {
        CookieFilter.FilterCookieHeader(header).Should().Equal(Filtered);
    }

    [Fact]
    public void FilterCookieHeader_NamelessCookieAmongPairs_FiltersOnlyThatElement()
    {
        var result = CookieFilter.FilterCookieHeader("opaque-token; theme=dark");

        result.Should().Equal(Filtered, "theme=dark");
    }

    [Theory]
    [InlineData("theme=dark, user_session=abc")]
    [InlineData("theme=dark mode")]
    [InlineData("the me=dark")]
    [InlineData("theme=\"dark")]
    [InlineData("theme=\"")]
    [InlineData(";;")]
    public void FilterCookieHeader_Malformed_FallsBackToSingleFilteredElement(string header)
    {
        CookieFilter.FilterCookieHeader(header).Should().Equal(Filtered);
    }

    [Theory]
    [InlineData("lang=en; theme=dark, user_session=abc; country=nz")]
    [InlineData("lang=en; the me=dark; country=nz")]
    [InlineData("lang=en; theme=\"dark; country=nz")]
    public void FilterCookieHeader_MalformedPairAmongValidPairs_FiltersOnlyThatElement(string header)
    {
        CookieFilter.FilterCookieHeader(header).Should().Equal("lang=en", Filtered, "country=nz");
    }

    [Fact]
    public void FilterCookieHeader_MultiplePairs_NeverContainsSensitiveValue()
    {
        var result = CookieFilter.FilterCookieHeader("user_session=s3cret;csrf_token=s3cret; theme=dark");

        result.Should().NotContain(element => element.Contains("s3cret"));
    }

    [Theory]
    [InlineData("__Host-prefs", "value")]
    [InlineData("__Secure-prefs", "value")]
    [InlineData("AWSALB", "value")]
    [InlineData("remember_me", "value")]
    [InlineData("USER_SESSION", "value")]
    public void FilterCookieHeader_SensitiveName_FiltersValueAndKeepsName(string name, string value)
    {
        var result = CookieFilter.FilterCookieHeader($"{name}={value}");

        result.Should().Equal($"{name}={Filtered}");
    }

    [Theory]
    [InlineData("theme=a=b", "theme=a=b")]
    [InlineData("theme=", "theme=")]
    [InlineData("theme=\"dark\"", "theme=\"dark\"")]
    [InlineData("theme = dark", "theme=dark")]
    [InlineData("theme=dark;", "theme=dark")]
    public void FilterCookieHeader_ValidPair_KeepsValue(string header, string expected)
    {
        CookieFilter.FilterCookieHeader(header).Should().Equal(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FilterCookieHeader_NoHeader_ReturnsEmpty(string header)
    {
        CookieFilter.FilterCookieHeader(header).Should().BeEmpty();
    }

    [Fact]
    public void FilterCookieHeader_Off_ReturnsEmpty()
    {
        CookieFilter.FilterCookieHeader("theme=dark", KeyValueCollectionBehavior.Off).Should().BeEmpty();
    }

    [Fact]
    public void FilterCookieHeader_DenyListTerm_FiltersMatchingName()
    {
        var result = CookieFilter.FilterCookieHeader("theme=dark; lang=en", KeyValueCollectionBehavior.DenyList("theme"));

        result.Should().Equal($"theme={Filtered}", "lang=en");
    }

    [Fact]
    public void FilterCookieHeader_AllowListTerm_KeepsOnlyMatchingValues()
    {
        var result = CookieFilter.FilterCookieHeader("theme=dark; lang=en", KeyValueCollectionBehavior.AllowList("theme"));

        result.Should().Equal("theme=dark", $"lang={Filtered}");
    }

    [Fact]
    public void FilterCookieHeaders_MultipleHeaders_CombinesCookies()
    {
        var result = CookieFilter.FilterCookieHeaders(["theme=dark", "user_session=abc"]);

        result.Should().Equal("theme=dark", $"user_session={Filtered}");
    }

    [Theory]
    [InlineData("Max-Age=3600")]
    [InlineData("Path=/")]
    [InlineData("Domain=example.com")]
    [InlineData("Expires=Wed, 21 Oct 2026 07:28:00 GMT")]
    [InlineData("SameSite=Lax")]
    [InlineData("Secure")]
    [InlineData("HttpOnly")]
    public void FilterSetCookieHeader_Attribute_IsNotACookie(string attribute)
    {
        var result = CookieFilter.FilterSetCookieHeader($"theme=light-mode; {attribute}");

        result.Should().Equal("theme=light-mode");
    }

    [Fact]
    public void FilterSetCookieHeader_NoAttributes_KeepsCookie()
    {
        CookieFilter.FilterSetCookieHeader("theme=light-mode").Should().Equal("theme=light-mode");
    }

    [Fact]
    public void FilterSetCookieHeader_SensitiveName_FiltersValue()
    {
        var result = CookieFilter.FilterSetCookieHeader("user_session=abc; Path=/; HttpOnly");

        result.Should().Equal($"user_session={Filtered}");
    }

    [Theory]
    [InlineData("opaque-token; Path=/")]
    [InlineData("=opaque-token; Path=/")]
    [InlineData("; Path=/")]
    [InlineData("theme=dark, user_session=abc; Path=/")]
    public void FilterSetCookieHeader_Unparseable_FallsBackToSingleFilteredElement(string header)
    {
        CookieFilter.FilterSetCookieHeader(header).Should().Equal(Filtered);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FilterSetCookieHeader_NoHeader_ReturnsEmpty(string header)
    {
        CookieFilter.FilterSetCookieHeader(header).Should().BeEmpty();
    }

    [Fact]
    public void FilterSetCookieHeader_Off_ReturnsEmpty()
    {
        CookieFilter.FilterSetCookieHeader("theme=dark", KeyValueCollectionBehavior.Off).Should().BeEmpty();
    }

    [Fact]
    public void FilterCookiePairs_NamelessAndSensitive_AreFiltered()
    {
        var result = CookieFilter.FilterCookiePairs(
        [
            new KeyValuePair<string, string>("user_session", "abc"),
            new KeyValuePair<string, string>("theme", "dark-mode"),
            new KeyValuePair<string, string>(null, "opaque-token"),
        ]);

        result.Should().Equal($"user_session={Filtered}", "theme=dark-mode", Filtered);
    }

    [Fact]
    public void FilterCookiePairs_NullValue_KeepsNameWithEmptyValue()
    {
        var result = CookieFilter.FilterCookiePairs([new KeyValuePair<string, string>("theme", null)]);

        result.Should().Equal("theme=");
    }

    [Fact]
    public void FilterCookiePairs_MalformedValue_FiltersOnlyThatElement()
    {
        var result = CookieFilter.FilterCookiePairs(
        [
            new KeyValuePair<string, string>("theme", "dark, user_session=abc"),
            new KeyValuePair<string, string>("lang", "en"),
        ]);

        result.Should().Equal(Filtered, "lang=en");
    }

    [Fact]
    public void FilterCookiePairs_Off_ReturnsEmpty()
    {
        var result = CookieFilter.FilterCookiePairs(
            [new KeyValuePair<string, string>("theme", "dark")],
            KeyValueCollectionBehavior.Off);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ToEventCookies_FilteredElements_JoinsThemIntoHeaderString()
    {
        var result = CookieFilter.ToEventCookies(["user_session=[Filtered]", "theme=dark-mode", "[Filtered]"]);

        result.Should().Be("user_session=[Filtered]; theme=dark-mode; [Filtered]");
    }

    [Fact]
    public void RequestCookieAttribute_SetOnTrace_SerializesAsStringArray()
    {
        var trace = new Trace();
        trace.SetData(CookieFilter.RequestCookieAttribute, new[] { "theme=dark-mode", Filtered });

        using var document = JsonDocument.Parse(trace.ToJsonString());
        var attribute = document.RootElement.GetProperty("data").GetProperty(CookieFilter.RequestCookieAttribute);

        attribute.EnumerateArray().Select(element => element.GetString()).Should().Equal("theme=dark-mode", Filtered);
    }
}
