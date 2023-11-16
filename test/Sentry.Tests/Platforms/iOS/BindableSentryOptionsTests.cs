using Microsoft.Extensions.Configuration;

namespace Sentry.Tests.Platforms.iOS;

public class BindableSentryOptionsTests
{
# if __IOS__
    [SkippableFact]
    public void ApplyTo_SetsiOSOptionsFromConfig()
    {
        var expected = new SentryOptions.IosOptions(new SentryOptions())
        {
            AttachScreenshot = true,
            AppHangTimeoutInterval = TimeSpan.FromSeconds(3),
            IdleTimeout = TimeSpan.FromSeconds(5),
            EnableAppHangTracking = true,
            EnableAutoBreadcrumbTracking = true,
            EnableAutoPerformanceTracing = true,
            EnableCoreDataTracing = true,
            EnableFileIOTracing = true,
            EnableNetworkBreadcrumbs = true,
            EnableNetworkTracking = true,
            EnableWatchdogTerminationTracking = true,
            EnableSwizzling = true,
            EnableUIViewControllerTracing = true,
            EnableUserInteractionTracing = true,
            EnableCocoaSdkTracing = true,
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AttachScreenshot"] = expected.AttachScreenshot.ToString(),
                ["AppHangTimeoutInterval"] = expected.AppHangTimeoutInterval.ToString(),
                ["IdleTimeout"] = expected.IdleTimeout.ToString(),
                ["EnableAppHangTracking"] = expected.EnableAppHangTracking.ToString(),
                ["EnableAutoBreadcrumbTracking"] = expected.EnableAutoBreadcrumbTracking.ToString(),
                ["EnableAutoPerformanceTracing"] = expected.EnableAutoPerformanceTracing.ToString(),
                ["EnableCoreDataTracing"] = expected.EnableCoreDataTracing.ToString(),
                ["EnableFileIOTracing"] = expected.EnableFileIOTracing.ToString(),
                ["EnableNetworkBreadcrumbs"] = expected.EnableNetworkBreadcrumbs.ToString(),
                ["EnableNetworkTracking"] = expected.EnableNetworkTracking.ToString(),
                ["EnableWatchdogTerminationTracking"] = expected.EnableWatchdogTerminationTracking.ToString(),
                ["EnableSwizzling"] = expected.EnableSwizzling.ToString(),
                ["EnableUIViewControllerTracing"] = expected.EnableUIViewControllerTracing.ToString(),
                ["EnableUserInteractionTracing"] = expected.EnableUserInteractionTracing.ToString(),
                ["EnableCocoaSdkTracing"] = expected.EnableCocoaSdkTracing.ToString(),
            }).Build();
        var bindable = new BindableSentryOptions.IosOptions();
        var actual = new SentryOptions.IosOptions(new SentryOptions());

        // Act
        config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        using (new AssertionScope())
        {
            actual.AttachScreenshot.Should().Be(expected.AttachScreenshot);
            actual.AppHangTimeoutInterval.Should().Be(expected.AppHangTimeoutInterval);
            actual.IdleTimeout.Should().Be(expected.IdleTimeout);
            actual.EnableAppHangTracking.Should().Be(expected.EnableAppHangTracking);
            actual.EnableAutoBreadcrumbTracking.Should().Be(expected.EnableAutoBreadcrumbTracking);
            actual.EnableAutoPerformanceTracing.Should().Be(expected.EnableAutoPerformanceTracing);
            actual.EnableCoreDataTracing.Should().Be(expected.EnableCoreDataTracing);
            actual.EnableFileIOTracing.Should().Be(expected.EnableFileIOTracing);
            actual.EnableNetworkBreadcrumbs.Should().Be(expected.EnableNetworkBreadcrumbs);
            actual.EnableNetworkTracking.Should().Be(expected.EnableNetworkTracking);
            actual.EnableWatchdogTerminationTracking.Should().Be(expected.EnableWatchdogTerminationTracking);
            actual.EnableSwizzling.Should().Be(expected.EnableSwizzling);
            actual.EnableUIViewControllerTracing.Should().Be(expected.EnableUIViewControllerTracing);
            actual.EnableUserInteractionTracing.Should().Be(expected.EnableUserInteractionTracing);
            actual.EnableCocoaSdkTracing.Should().Be(expected.EnableCocoaSdkTracing);
        }
    }
#endif
}
