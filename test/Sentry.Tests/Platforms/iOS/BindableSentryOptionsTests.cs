# if __IOS__
using Microsoft.Extensions.Configuration;

namespace Sentry.Tests.Platforms.Cocoa;

public class BindableSentryOptionsTests : BindableTests<SentryOptions.CocoaOptions>
{
    public BindableSentryOptionsTests()
    : base(nameof(SentryOptions.CocoaOptions.UrlSessionDelegate))
    {
    }

    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var actual = GetPropertyNames<BindableSentryOptions.CocoaOptions>();
        AssertContainsAllOptionsProperties(actual);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new SentryOptions.CocoaOptions(new SentryOptions());
        var bindable = new BindableSentryOptions.CocoaOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }
}
#endif
