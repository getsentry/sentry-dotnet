namespace Sentry.Testing;

public static class SentryAttributesExtensions
{
    internal static void ShouldContain<T>(this SentryAttributes attributes, string key, T expected)
    {
        attributes.TryGetAttribute<T>(key, out var value).Should().BeTrue();
        value.Should().Be(expected);
    }

    internal static void ShouldNotContain<T>(this SentryAttributes attributes, string key)
    {
        attributes.TryGetAttribute<T>(key, out var value).Should().BeFalse();
        value.Should().Be(default(T));
    }
}
