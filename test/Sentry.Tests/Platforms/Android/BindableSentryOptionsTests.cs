using Microsoft.Extensions.Configuration;

namespace Sentry.Tests.Platforms.Android;

public class BindableSentryOptionsTests
{
#if ANDROID
    [SkippableFact]
    public void ApplyTo_SetsAndroidOptionsFromConfig()
    {
        var expected = new SentryOptions.AndroidOptions(new SentryOptions())
        {
            AnrEnabled = true,
            AnrReportInDebug = true,
            AnrTimeoutInterval = TimeSpan.FromSeconds(3),
            AttachScreenshot = true,
            EnableActivityLifecycleBreadcrumbs = true,
            EnableAppComponentBreadcrumbs = true,
            EnableAppLifecycleBreadcrumbs = true,
            EnableRootCheck = true,
            EnableSystemEventBreadcrumbs = true,
            EnableUserInteractionBreadcrumbs = true,
            EnableAutoActivityLifecycleTracing = true,
            EnableActivityLifecycleTracingAutoFinish = true,
            EnableUserInteractionTracing = true,
            AttachThreads = true,
            ConnectionTimeout = TimeSpan.FromSeconds(7),
            EnableNdk = true,
            EnableShutdownHook = true,
            EnableUncaughtExceptionHandler = true,
            PrintUncaughtStackTrace = true,
            ReadTimeout = TimeSpan.FromSeconds(13),
            EnableAndroidSdkTracing = true,
            EnableAndroidSdkBeforeSend = true,
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AnrEnabled"] = expected.AnrEnabled.ToString(),
                ["AnrReportInDebug"] = expected.AnrReportInDebug.ToString(),
                ["AnrTimeoutInterval"] = expected.AnrTimeoutInterval.ToString(),
                ["AttachScreenshot"] = expected.AttachScreenshot.ToString(),
                ["EnableActivityLifecycleBreadcrumbs"] = expected.EnableActivityLifecycleBreadcrumbs.ToString(),
                ["EnableAppComponentBreadcrumbs"] = expected.EnableAppComponentBreadcrumbs.ToString(),
                ["EnableAppLifecycleBreadcrumbs"] = expected.EnableAppLifecycleBreadcrumbs.ToString(),
                ["EnableRootCheck"] = expected.EnableRootCheck.ToString(),
                ["EnableSystemEventBreadcrumbs"] = expected.EnableSystemEventBreadcrumbs.ToString(),
                ["EnableUserInteractionBreadcrumbs"] = expected.EnableUserInteractionBreadcrumbs.ToString(),
                ["EnableAutoActivityLifecycleTracing"] = expected.EnableAutoActivityLifecycleTracing.ToString(),
                ["EnableActivityLifecycleTracingAutoFinish"] = expected.EnableActivityLifecycleTracingAutoFinish.ToString(),
                ["EnableUserInteractionTracing"] = expected.EnableUserInteractionTracing.ToString(),
                ["AttachThreads"] = expected.AttachThreads.ToString(),
                ["ConnectionTimeout"] = expected.ConnectionTimeout.ToString(),
                ["EnableNdk"] = expected.EnableNdk.ToString(),
                ["EnableShutdownHook"] = expected.EnableShutdownHook.ToString(),
                ["EnableUncaughtExceptionHandler"] = expected.EnableUncaughtExceptionHandler.ToString(),
                ["PrintUncaughtStackTrace"] = expected.PrintUncaughtStackTrace.ToString(),
                ["ReadTimeout"] = expected.ReadTimeout.ToString(),
                ["EnableAndroidSdkTracing"] = expected.EnableAndroidSdkTracing.ToString(),
                ["EnableAndroidSdkBeforeSend"] = expected.EnableAndroidSdkBeforeSend.ToString()
            }).Build();
        var bindable = new BindableSentryOptions.AndroidOptions();
        var actual = new SentryOptions.AndroidOptions(new SentryOptions());

        // Act
        config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        using (new AssertionScope())
        {
            actual.AnrEnabled.Should().Be(expected.AnrEnabled);
            actual.AnrReportInDebug.Should().Be(expected.AnrReportInDebug);
            actual.AnrTimeoutInterval.Should().Be(expected.AnrTimeoutInterval);
            actual.AttachScreenshot.Should().Be(expected.AttachScreenshot);
            actual.EnableActivityLifecycleBreadcrumbs.Should().Be(expected.EnableActivityLifecycleBreadcrumbs);
            actual.EnableAppComponentBreadcrumbs.Should().Be(expected.EnableAppComponentBreadcrumbs);
            actual.EnableAppLifecycleBreadcrumbs.Should().Be(expected.EnableAppLifecycleBreadcrumbs);
            actual.EnableRootCheck.Should().Be(expected.EnableRootCheck);
            actual.EnableSystemEventBreadcrumbs.Should().Be(expected.EnableSystemEventBreadcrumbs);
            actual.EnableUserInteractionBreadcrumbs.Should().Be(expected.EnableUserInteractionBreadcrumbs);
            actual.EnableAutoActivityLifecycleTracing.Should().Be(expected.EnableAutoActivityLifecycleTracing);
            actual.EnableActivityLifecycleTracingAutoFinish.Should().Be(expected.EnableActivityLifecycleTracingAutoFinish);
            actual.EnableUserInteractionTracing.Should().Be(expected.EnableUserInteractionTracing);
            actual.AttachThreads.Should().Be(expected.AttachThreads);
            actual.ConnectionTimeout.Should().Be(expected.ConnectionTimeout);
            actual.EnableNdk.Should().Be(expected.EnableNdk);
            actual.EnableShutdownHook.Should().Be(expected.EnableShutdownHook);
            actual.EnableUncaughtExceptionHandler.Should().Be(expected.EnableUncaughtExceptionHandler);
            actual.PrintUncaughtStackTrace.Should().Be(expected.PrintUncaughtStackTrace);
            actual.ReadTimeout.Should().Be(expected.ReadTimeout);
            actual.EnableAndroidSdkTracing.Should().Be(expected.EnableAndroidSdkTracing);
            actual.EnableAndroidSdkBeforeSend.Should().Be(expected.EnableAndroidSdkBeforeSend);
        }
    }
#endif
}
