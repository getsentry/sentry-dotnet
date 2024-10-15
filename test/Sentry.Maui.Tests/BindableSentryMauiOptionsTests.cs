#if !NETFRAMEWORK
using Microsoft.Extensions.Configuration;

namespace Sentry.Maui.Tests;

public class BindableSentryMauiOptionsTests : BindableTests<SentryMauiOptions>
{
    public BindableSentryMauiOptionsTests() : base(
        nameof(SentryOptions.ExperimentalMetrics)
#if NET6_0_OR_GREATER && !(IOS || ANDROID)
        , nameof(SentryOptions.HeapDumpDebouncer)
        , nameof(SentryOptions.HeapDumpTrigger)
#endif
    )
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
