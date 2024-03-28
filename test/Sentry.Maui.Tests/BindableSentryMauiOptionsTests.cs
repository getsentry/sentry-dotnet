#if !NETFRAMEWORK
using Microsoft.Extensions.Configuration;

namespace Sentry.Maui.Tests;

public class BindableSentryMauiOptionsTests : BindableTests<SentryMauiOptions>
{
#pragma warning disable CS0618 // Type or member is obsolete
    public BindableSentryMauiOptionsTests() : base(nameof(SentryMauiOptions.ExperimentalMetrics), nameof(SentryMauiOptions.Metrics))
#pragma warning restore CS0618 // Type or member is obsolete
    {
    }

    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var actual = GetPropertyNames<BindableSentryMauiOptions>();
        AssertContainsAllOptionsProperties(actual);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new SentryMauiOptions();
        var bindable = new BindableSentryMauiOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }
}
#endif
