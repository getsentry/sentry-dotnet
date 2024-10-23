#if !NETFRAMEWORK
using Microsoft.Extensions.Configuration;

namespace Sentry.AspNetCore.Tests;

public class BindableSentryAspNetCoreOptionsTests : BindableTests<SentryAspNetCoreOptions>
{
    public BindableSentryAspNetCoreOptionsTests() : base(
        nameof(SentryOptions.ExperimentalMetrics)
#if MEMORY_DUMP_SUPPORTED
        , nameof(SentryOptions.HeapDumpDebouncer)
        , nameof(SentryOptions.HeapDumpTrigger)
#endif
    )
    {
    }

    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var actual = GetPropertyNames<BindableSentryAspNetCoreOptions>();
        AssertContainsAllOptionsProperties(actual);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new SentryAspNetCoreOptions();
        var bindable = new BindableSentryAspNetCoreOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }
}
#endif
