#if NET8_0_OR_GREATER
using Microsoft.Extensions.Configuration;

namespace Sentry.Azure.Functions.Worker.Tests;

public class BindableSentryAzureFunctionsOptionsTests : BindableTests<SentryAzureFunctionsOptions>
{
    public BindableSentryAzureFunctionsOptionsTests() : base(
        nameof(SentryOptions.ExperimentalMetrics)
    )
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
