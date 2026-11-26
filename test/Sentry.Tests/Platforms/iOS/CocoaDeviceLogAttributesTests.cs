#if IOS
using Sentry.Cocoa;

namespace Sentry.Tests.Platforms.iOS;

public class CocoaDeviceLogAttributesTests
{
    [Fact]
    public void Create_NativeContexts_MapsDeviceAndOs()
    {
        var contexts = new SentryContexts();
        contexts.OperatingSystem.Name = "iOS";
        contexts.OperatingSystem.Version = "26.5";
        contexts.Device.Model = "iPhone17,1";
        contexts.Device.Family = "iOS";

        var attributes = CocoaDeviceLogAttributes.Create(contexts);

        attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["os.name"] = "iOS",
            ["os.version"] = "26.5",
            ["device.brand"] = "Apple",
            ["device.model"] = "iPhone17,1",
            ["device.family"] = "iOS",
        });
    }
}
#endif
