using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests.Internal;

public class SentryMauiOptionsSetupTests
{
    [Fact]
    public void Configure_DeviceLogAttributes_UnknownPlatformYieldsNone()
    {
        var options = new SentryMauiOptions
        {
            NetworkStatusListener = FakeReliableNetworkStatusListener.Instance
        };

        new SentryMauiOptionsSetup().Configure(options);

        options.DeviceLogAttributes.Should().NotBeNull();
        options.DeviceLogAttributes!.Value.Should().BeEmpty();
    }
}
