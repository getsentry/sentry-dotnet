namespace Sentry.Testing;

internal static class SentryAttributesExtensions
{
    internal static void ShouldContain<T>(this SentryAttributes attributes, string key, T expected)
    {
        attributes.TryGetValue(key, out var value).Should().BeTrue();
        value.Should().Be(expected);
    }
}
