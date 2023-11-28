# if __IOS__
using Microsoft.Extensions.Configuration;

namespace Sentry.Tests.Platforms.iOS;

public class BindableSentryOptionsTests : BindableTests<SentryOptions.IosOptions>
{
    public BindableSentryOptionsTests()
    : base(nameof(SentryOptions.IosOptions.UrlSessionDelegate))
    {
    }

    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var actual = GetPropertyNames<BindableSentryOptions.IosOptions>();
        AssertContainsAllOptionsProperties(actual);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new SentryOptions.IosOptions(new SentryOptions());
        var bindable = new BindableSentryOptions.IosOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }
}
#endif
