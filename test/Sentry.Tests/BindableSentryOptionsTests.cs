#if !NETFRAMEWORK
using Microsoft.Extensions.Configuration;

namespace Sentry.Tests;

public class BindableSentryOptionsTests : BindableTests<SentryOptions>
{
    public BindableSentryOptionsTests() : base(
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
