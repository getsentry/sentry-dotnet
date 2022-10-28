using Microsoft.Maui.TestUtils.DeviceTests.Runners;

namespace Sentry.Maui.Device.TestApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var appBuilder = MauiApp.CreateBuilder()
            .ConfigureTests(new TestOptions
            {
                // This is the list of assemblies containing tests that will be run
                Assemblies =
                {
                    typeof(Sentry.Tests.SentrySdkTests).Assembly,
                    typeof(Sentry.Extensions.Logging.Tests.LogLevelExtensionsTests).Assembly,
                    typeof(Sentry.Maui.Tests.MauiNetworkStatusListenerTests).Assembly
                },
                SkipCategories = new List<string>
                {
                    // Tests that use Verify can't run on the device because the verification files are not present.
                    "Category=Verify",

                    // Tests that we have haven't validated for device tests
                    "Category=DeviceUnvalidated"
                }
            })
            .UseHeadlessRunner(new HeadlessRunnerOptions
            {
                RequiresUIContext = true,
            })
            .UseVisualRunner();

        return appBuilder.Build();
    }
}
