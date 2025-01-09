# if __IOS__
using Microsoft.Extensions.Configuration;

namespace Sentry.Tests.Platforms.Cocoa;

public class BindableSentryOptionsTests : BindableTests<SentryOptions.NativeOptions>
{
    public BindableSentryOptionsTests()
    : base(
        nameof(SentryOptions.NativeOptions.UrlSessionDelegate),
        nameof(SentryOptions.Native.SessionReplay))
    {
    }

    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var actual = GetPropertyNames<BindableSentryOptions.NativeOptions>();
        AssertContainsAllOptionsProperties(actual);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new SentryOptions.NativeOptions(new SentryOptions());
        var bindable = new BindableSentryOptions.NativeOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }
}
#endif
