#if NET8_0_OR_GREATER
using Microsoft.Extensions.Configuration;

namespace Sentry.Azure.Functions.Worker.Tests;

public class BindableSentryAzureFunctionsOptionsTests : BindableTests<SentryAzureFunctionsOptions>
{
#pragma warning disable CS0618 // Type or member is obsolete
    public BindableSentryAzureFunctionsOptionsTests() : base(nameof(SentryAzureFunctionsOptions.ExperimentalMetrics), nameof(SentryAzureFunctionsOptions.Metrics))
#pragma warning restore CS0618 // Type or member is obsolete
    {
    }

    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var actual = GetPropertyNames<BindableSentryAzureFunctionsOptions>();
        AssertContainsAllOptionsProperties(actual);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new SentryAzureFunctionsOptions();
        var bindable = new BindableSentryAzureFunctionsOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }
}
#endif
