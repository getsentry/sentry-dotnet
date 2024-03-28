#if !NETFRAMEWORK
using Microsoft.Extensions.Configuration;

namespace Sentry.Tests;

public class BindableSentryOptionsTests : BindableTests<SentryOptions>
{
#pragma warning disable CS0618 // Type or member is obsolete
    public BindableSentryOptionsTests() : base(nameof(SentryOptions.ExperimentalMetrics), nameof(SentryOptions.Metrics))
#pragma warning restore CS0618 // Type or member is obsolete
    {
    }

    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var actual = GetPropertyNames<BindableSentryOptions>();
        AssertContainsAllOptionsProperties(actual);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new SentryOptions();
        var bindable = new BindableSentryOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }
}
#endif
