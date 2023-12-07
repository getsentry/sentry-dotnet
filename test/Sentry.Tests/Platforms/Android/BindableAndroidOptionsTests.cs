#if ANDROID
using Microsoft.Extensions.Configuration;

namespace Sentry.Tests.Platforms.Android;

public class BindableAndroidOptionsTests : BindableTests<SentryOptions.AndroidOptions>
{
    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var actual = GetPropertyNames<BindableSentryOptions.AndroidOptions>();
        AssertContainsAllOptionsProperties(actual);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new SentryOptions.AndroidOptions();
        var bindable = new BindableSentryOptions.AndroidOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }
}
#endif
