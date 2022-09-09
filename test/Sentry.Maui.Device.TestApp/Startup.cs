using Microsoft.Maui.TestUtils.DeviceTests.Runners;

namespace Sentry.Maui.Device.TestApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var appBuilder = MauiApp.CreateBuilder();
            appBuilder
                .ConfigureTests(new TestOptions
                {
                    // This is the list of assemblies containing tests that will be run
                    Assemblies =
                    {
                        // TODO: validate tests
                        // typeof(Sentry.Tests.SentrySdkTests).Assembly,
                        // typeof(Sentry.Extensions.Logging.Tests.LogLevelExtensionsTests).Assembly,

                        typeof(Sentry.Maui.Tests.MauiNetworkStatusListenerTests).Assembly
                    },
                })
                .UseHeadlessRunner(new HeadlessRunnerOptions
                {
                    RequiresUIContext = true,
                })
                .UseVisualRunner();

            return appBuilder.Build();
        }
    }
}
