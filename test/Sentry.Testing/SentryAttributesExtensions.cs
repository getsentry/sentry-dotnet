namespace Sentry.Testing;

internal static class SentryAttributesExtensions
{
    internal static void ShouldContain<T>(this SentryAttributes attributes, string key, T expected)
    {
        attributes.TryGetAttribute<T>(key, out var value).Should().BeTrue();
        value.Should().Be(expected);
    }

    internal static void AssertContains<T>(this SentryAttributes attributes, string key, T expected)
    {
        var hasAttribute = attributes.TryGetAttribute<T>(key, out var value);
        Assert.True(hasAttribute);
        Assert.Equal(expected, value);
    }
}
