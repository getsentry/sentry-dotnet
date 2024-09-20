using DeviceRunners.XHarness;
using Microsoft.Extensions.Logging;

namespace Sentry.Maui.Device.TestApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseXHarnessTestRunner(conf =>
            {
                conf.AddTestAssemblies([
                    typeof(Sentry.Tests.SentrySdkTests).Assembly,
                    typeof(Sentry.Extensions.Logging.Tests.LogLevelExtensionsTests).Assembly,
                    typeof(Sentry.Maui.Tests.SentryMauiOptionsTests).Assembly,
#if ANDROID
                    typeof(Sentry.Android.AssemblyReader.Tests.AndroidAssemblyReaderTests).Assembly,
#endif
                ]);
                conf.AddXunit();
            });

        return builder.Build();
    }
}
