#if !NETFRAMEWORK
using Microsoft.Extensions.Configuration;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggingOptionsTests : BindableTests<SentryLoggingOptions>
{
    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var propertyNames = GetPropertyNames<BindableSentryLoggingOptions>();
        AssertContainsAllOptionsProperties(propertyNames);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new SentryLoggingOptions();
        var bindable = new BindableSentryLoggingOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }
}
#endif

