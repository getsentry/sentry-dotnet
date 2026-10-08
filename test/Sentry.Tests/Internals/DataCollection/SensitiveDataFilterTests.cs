using Sentry.Internal.DataCollection;

namespace Sentry.Tests.Internals.DataCollection;

public class SensitiveDataFilterTests
{
    [Theory]
    [InlineData("auth")]
    [InlineData("token")]
    [InlineData("secret")]
    [InlineData("session")]
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
    [InlineData("sid")]
    [InlineData("identity")]
    [InlineData("set-cookie")]
    [InlineData("cookie")]
    public void IsSensitiveKey_CanonicalDenylistTerm_ReturnsTrue(string term)
    {
        SensitiveDataFilter.IsSensitiveKey(term).Should().BeTrue();
    }

    [Theory]
    [InlineData("Authorization")] // contains "auth"
    [InlineData("X-Api-Key")] // contains "key"
    [InlineData("PHPSESSID")] // contains "sid"
    [InlineData("X-CSRF-TOKEN")]
    [InlineData("Set-Cookie")]
    public void IsSensitiveKey_KeyContainingTermInAnyCase_ReturnsTrue(string key)
    {
        SensitiveDataFilter.IsSensitiveKey(key).Should().BeTrue();
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
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueFilterBehavior.Off);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterKeyValueData_DefaultBehavior_IsOff()
    {
        // The default struct value must be the most conservative mode
        default(KeyValueFilterBehavior).Mode.Should().Be(KeyValueFilterMode.Off);
        default(KeyValueFilterBehavior).Terms.Should().BeEmpty();
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
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueFilterBehavior.DenyList);

        // Assert
        result.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["User-Agent"] = "TestAgent",
            ["Authorization"] = "[Filtered]",
            ["X-API-KEY"] = "[Filtered]"
        });
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
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueFilterBehavior.DenyListWith("forwarded"));

        // Assert
        result["X-Forwarded-For"].Should().Be("[Filtered]");
        result["Accept"].Should().Be("application/json");
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
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueFilterBehavior.AllowList("user-agent"));

        // Assert
        result.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["User-Agent"] = "TestAgent",
            ["Accept"] = "[Filtered]",
            ["X-Custom"] = "[Filtered]"
        });
    }

    [Fact]
    public void FilterKeyValueData_AllowListMode_BuiltInDenylistWins()
    {
        // Arrange: allow-listing a sensitive key must not expose its value
        var data = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer 123"
        };

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueFilterBehavior.AllowList("authorization"));

        // Assert
        result["Authorization"].Should().Be("[Filtered]");
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
        var result = SensitiveDataFilter.FilterKeyValueData(data, KeyValueFilterBehavior.AllowList("tenant"));

        // Assert
        result["X-Tenant-Id"].Should().Be("acme");
        result["X-Request-Id"].Should().Be("[Filtered]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FilterKeyValueData_AdditionalDenyTerms_AppliedInEveryMode(bool useAllowList)
    {
        // Arrange: "remember" is a cookie-name term, not part of the built-in denylist
        var data = new Dictionary<string, string>
        {
            ["remember_me"] = "yes"
        };
        var behavior = useAllowList
            ? KeyValueFilterBehavior.AllowList("remember_me")
            : KeyValueFilterBehavior.DenyList;

        // Act
        var result = SensitiveDataFilter.FilterKeyValueData(
            data, behavior, SensitiveDataFilter.SensitiveCookieNameTerms);

        // Assert
        result["remember_me"].Should().Be("[Filtered]");
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
            data, KeyValueFilterBehavior.DenyList, SensitiveDataFilter.SensitiveCookieNameTerms);

        // Assert
        result[cookieName].Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterValue_BenignKeyDenyListMode_ReturnsValueUnchanged()
    {
        SensitiveDataFilter.FilterValue("Accept", "text/html", KeyValueFilterBehavior.DenyList)
            .Should().Be("text/html");
    }

    [Theory]
    [InlineData("X-Forwarded-For")]
    [InlineData("Client-IP")]
    [InlineData("Remote-Addr")]
    [InlineData("Via")]
    [InlineData("X-Real-User")]
    public void FilterValue_PiiKeyTermsAsDenyTerms_Filtered(string key)
    {
        // The GDPR terms are not filtered by default but must work as extra deny terms (SendDefaultPii=false bridge)
        SensitiveDataFilter.FilterValue(key, "value", KeyValueFilterBehavior.DenyList, SensitiveDataFilter.PiiKeyTerms)
            .Should().Be("[Filtered]");
    }

    [Fact]
    public void FilterValue_PiiKeyTermsNotPassed_NotFilteredByDefault()
    {
        SensitiveDataFilter.FilterValue("X-Forwarded-For", "203.0.113.7", KeyValueFilterBehavior.DenyList)
            .Should().Be("203.0.113.7");
    }

    [Fact]
    public void FilterKeyValueData_EmptyData_ReturnsEmpty()
    {
        SensitiveDataFilter.FilterKeyValueData([], KeyValueFilterBehavior.DenyList).Should().BeEmpty();
    }
}
