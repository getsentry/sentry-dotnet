#if !NETFRAMEWORK
using Microsoft.Extensions.Configuration;

namespace Sentry.AspNetCore.Tests;

public class BindableSentryAspNetCoreOptionsTests : BindableTests<SentryAspNetCoreOptions>
{
#pragma warning disable CS0618 // Type or member is obsolete
    public BindableSentryAspNetCoreOptionsTests() : base(nameof(SentryAspNetCoreOptions.ExperimentalMetrics), nameof(SentryAspNetCoreOptions.Metrics))
#pragma warning restore CS0618 // Type or member is obsolete
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
