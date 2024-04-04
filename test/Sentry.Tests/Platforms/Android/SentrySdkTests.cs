using Sentry.JavaSdk.Android.Core;

namespace Sentry.Tests.Platforms.Android;

public class SentrySdkTests
{
    [Fact]
    public void ApplySentryOptions_AppliesSampleRate()
    {
        // Arrange
        var sentryOptions = new SentryOptions
        {
            SampleRate = 0.5F
        };
        var androidOptions = new SentryAndroidOptions();

        // Act
        sentryOptions.ApplySentryOptions(androidOptions);

        // Assert
        androidOptions.SampleRate.Should().Be(0.5F);
    }
}
