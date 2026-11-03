using Sentry.Internal.DataCollection;

namespace Sentry.Tests.Internals.DataCollection;

public class SensitiveDataFilterTests
{
    private static string ValueOf(List<KeyValuePair<string, string>> data, string key) =>
        data.Single(kvp => kvp.Key == key).Value;

    [Theory]
    [InlineData("auth")]
    [InlineData("token")]
    [InlineData("secret")]
    [InlineData("password")]
    [InlineData("passwd")]
    [InlineData("pwd")]
    [InlineData("key")]
    [InlineData("jwt")]
    [InlineData("bearer")]
    [InlineData("sso")]
    [InlineData("saml")]
    [InlineData("csrf")]
    [InlineData("xsrf")]
    [InlineData("credentials")]
    [InlineData("session")]
    [InlineData("sid")]
    [InlineData("identity")]
    public void IsSensitiveKey_CanonicalDenylistTerm_ReturnsTrue(string term)
    {
        SensitiveDataFilter.IsSensitiveKey(term).Should().BeTrue();
    }

    [Fact]
    public void SensitiveKeyTerms_MatchesSpecDenylistExactly()
    {
        SensitiveDataFilter.SensitiveKeyTerms.Should().Equal(
            "auth", "token", "secret", "password", "passwd", "pwd", "key", "jwt", "bearer",
            "sso", "saml", "csrf", "xsrf", "credentials", "session", "sid", "identity");
    }

    [Theory]
    [InlineData("Authorization")]
    [InlineData("X-Api-Key")]
    [InlineData("PHPSESSID")]
    [InlineData("connect.sid")]
    [InlineData("X-CSRF-TOKEN")]
    public void IsSensitiveKey_KeyContainingTermInAnyCase_ReturnsTrue(string key)
    {
        SensitiveDataFilter.IsSensitiveKey(key).Should().BeTrue();
    }

    [Theory]
    [InlineData("Cookie")]
    [InlineData("Set-Cookie")]
    [InlineData("cookie_consent")]
    public void IsSensitiveKey_CookieNames_ReturnsFalse(string key)
    {
        SensitiveDataFilter.IsSensitiveKey(key).Should().BeFalse();
    }

    [Theory]
    [InlineData("User-Agent")]
    [InlineData("Accept")]
    [InlineData("Content-Type")]
    [InlineData("traceparent")]
    public void IsSensitiveKey_BenignKey_ReturnsFalse(string key)
    {
        SensitiveDataFilter.IsSensitiveKey(key).Should().BeFalse();
    }

    [Fact]
    public void FilterKeyValueData_OffMode_ReturnsEmpty()
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            ["User-Agent"] = "TestAgent",
            ["Authorization"] = "Bearer 123"
        };

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueCollectionBehavior.Off);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterKeyValueData_DenyListMode_FiltersSensitiveValuesAndPreservesKeys()
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            ["User-Agent"] = "TestAgent",
            ["Authorization"] = "Bearer 123",
            ["X-API-KEY"] = "abc123"
        };

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueCollectionBehavior.DenyList());

        // Assert
        ValueOf(result, "User-Agent").Should().Be("TestAgent");
        ValueOf(result, "Authorization").Should().Be("[Filtered]");
        ValueOf(result, "X-API-KEY").Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterKeyValueData_PreservesInputOrderAndRepeatedKeys()
    {
        // Arrange
        var data = new[]
        {
            new KeyValuePair<string, string>("Set-Cookie", "a=1"),
            new KeyValuePair<string, string>("Accept", "text/html"),
            new KeyValuePair<string, string>("Set-Cookie", "b=2")
        };

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueCollectionBehavior.DenyList());

        // Assert
        result.Select(kvp => kvp.Key).Should().Equal("Set-Cookie", "Accept", "Set-Cookie");
        result.Select(kvp => kvp.Value).Should().Equal("a=1", "text/html", "b=2");
    }

    [Fact]
    public void FilterKeyValueData_DenyListModeWithExtraTerms_FiltersExtraTerms()
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            ["X-Forwarded-For"] = "203.0.113.7",
            ["Accept"] = "application/json"
        };

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueCollectionBehavior.DenyList("forwarded"));

        // Assert
        ValueOf(result, "X-Forwarded-For").Should().Be("[Filtered]");
        ValueOf(result, "Accept").Should().Be("application/json");
    }

    [Fact]
    public void FilterKeyValueData_AllowListMode_OnlyAllowedKeysKeepValues()
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            ["User-Agent"] = "TestAgent",
            ["Accept"] = "application/json",
            ["X-Custom"] = "value"
        };

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueCollectionBehavior.AllowList("user-agent"));

        // Assert
        ValueOf(result, "User-Agent").Should().Be("TestAgent");
        ValueOf(result, "Accept").Should().Be("[Filtered]");
        ValueOf(result, "X-Custom").Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterKeyValueData_AllowListMode_BuiltInDenylistWins()
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer 123"
        };

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueCollectionBehavior.AllowList("authorization"));

        // Assert
        ValueOf(result, "Authorization").Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterKeyValueData_AllowListMode_MatchesSubstrings()
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            ["X-Tenant-Id"] = "acme",
            ["X-Request-Id"] = "42"
        };

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueCollectionBehavior.AllowList("tenant"));

        // Assert
        ValueOf(result, "X-Tenant-Id").Should().Be("acme");
        ValueOf(result, "X-Request-Id").Should().Be("[Filtered]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FilterKeyValueData_AdditionalDenyTerms_AppliedInEveryMode(bool useAllowList)
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            ["remember_me"] = "yes"
        };
        var behavior = useAllowList
            ? KeyValueCollectionBehavior.AllowList("remember_me")
            : KeyValueCollectionBehavior.DenyList();

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(
            data, behavior, SensitiveDataFilter.SensitiveCookieNameTerms);

        // Assert
        ValueOf(result, "remember_me").Should().Be("[Filtered]");
    }

    [Theory]
    [InlineData("connect.sid")]
    [InlineData("PHPSESSID")]
    [InlineData("remember_token")]
    [InlineData("__Secure-next-auth.session-token")]
    [InlineData("__Host-csrf")]
    [InlineData("AWSALB")]
    [InlineData("__stripe_mid")]
    [InlineData("CognitoIdentityServiceProvider.foo")]
    [InlineData("sb-access-token")]
    [InlineData("mfa_pending")]
    public void FilterKeyValueData_CommonSessionCookieNames_FilteredWithCookieTerms(string cookieName)
    {
        // Arrange
        var data = new Dictionary<string, string> { [cookieName] = "value" };

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(
            data, KeyValueCollectionBehavior.DenyList(), SensitiveDataFilter.SensitiveCookieNameTerms);

        // Assert
        ValueOf(result, cookieName).Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterValue_OffMode_ReturnsFiltered()
    {
        SensitiveDataFilter.FilterValue("Accept", "text/html", KeyValueCollectionBehavior.Off)
            .Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterValue_BenignKeyDenyListMode_ReturnsValueUnchanged()
    {
        SensitiveDataFilter.FilterValue("Accept", "text/html", KeyValueCollectionBehavior.DenyList())
            .Should().Be("text/html");
    }

    [Fact]
    public void FilterValue_DenyListWithEmptyTerm_DoesNotFilterEverything()
    {
        SensitiveDataFilter.FilterValue("Accept", "text/html", KeyValueCollectionBehavior.DenyList(""))
            .Should().Be("text/html");
    }

    [Fact]
    public void FilterValue_AllowListWithEmptyTerm_DoesNotAllowEverything()
    {
        SensitiveDataFilter.FilterValue("Accept", "text/html", KeyValueCollectionBehavior.AllowList(""))
            .Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterValue_DenyListWithNullTerm_DoesNotFilterEverything()
    {
        SensitiveDataFilter.FilterValue("Accept", "text/html", KeyValueCollectionBehavior.DenyList(null!))
            .Should().Be("text/html");
    }

    [Fact]
    public void FilterValue_AllowListWithNullTermsArray_DoesNotAllowEverything()
    {
        SensitiveDataFilter.FilterValue("Accept", "text/html", KeyValueCollectionBehavior.AllowList(null!))
            .Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterValue_DenyListWithNullTermElement_DoesNotFilterEverything()
    {
        var behavior = KeyValueCollectionBehavior.DenyList(new string[] { null! });

        SensitiveDataFilter.FilterValue("Accept", "text/html", behavior).Should().Be("text/html");
    }

    [Fact]
    public void FilterValue_AllowListWithNullTermElement_DoesNotAllowEverything()
    {
        var behavior = KeyValueCollectionBehavior.AllowList(new string[] { null! });

        SensitiveDataFilter.FilterValue("Accept", "text/html", behavior).Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterValue_EncodedSensitiveKey_FilteredWhenDecodedForMatching()
    {
        // Arrange
        const string encodedKey = "%74oken";

        // Act
        var key = SensitiveDataFilter.DecodeKeyForMatching(encodedKey);

        // Assert
        key.Should().Be("token");
        SensitiveDataFilter.FilterValue(key, "abc123", KeyValueCollectionBehavior.DenyList())
            .Should().Be("[Filtered]");
    }

    [Fact]
    public void DecodeKeyForMatching_MalformedEscape_ReturnsOriginal()
    {
        SensitiveDataFilter.DecodeKeyForMatching("%zz").Should().Be("%zz");
    }

    [Theory]
    [InlineData("X-Forwarded-For")]
    [InlineData("Client-IP")]
    [InlineData("Remote-Addr")]
    [InlineData("Via")]
    [InlineData("X-Real-User")]
    public void FilterValue_PiiKeyTermsAsDenyTerms_Filtered(string key)
    {
        SensitiveDataFilter.FilterValue(key, "value", KeyValueCollectionBehavior.DenyList(), SensitiveDataFilter.PiiKeyTerms)
            .Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterValue_PiiKeyTermsNotPassed_NotFilteredByDefault()
    {
        SensitiveDataFilter.FilterValue("X-Forwarded-For", "203.0.113.7", KeyValueCollectionBehavior.DenyList())
            .Should().Be("203.0.113.7");
    }

    [Fact]
    public void FilterKeyValueData_EmptyData_ReturnsEmpty()
    {
        SensitiveDataFilter.FilterKeyValueData([], KeyValueCollectionBehavior.DenyList()).Should().BeEmpty();
    }
}
