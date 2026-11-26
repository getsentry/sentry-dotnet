using Sentry.Protocol;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.Tests;

public class DeviceLogAttributesTests
{
    [Fact]
    public void Create_AllValues_MapsToSpecAttributes()
    {
        var os = new OperatingSystem { Name = "Android", Version = "15" };
        var device = new Device { Brand = "google", Model = "Pixel 9", Family = "Pixel" };

        var attributes = DeviceLogAttributes.Create(os, device);

        attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["os.name"] = "Android",
            ["os.version"] = "15",
            ["device.brand"] = "google",
            ["device.model"] = "Pixel 9",
            ["device.family"] = "Pixel",
        });
    }

    [Fact]
    public void Create_BlankValues_OmitsThem()
    {
        var os = new OperatingSystem { Name = "Android", Version = " " };
        var device = new Device { Model = "" };

        var attributes = DeviceLogAttributes.Create(os, device);

        attributes.Should().BeEquivalentTo(new Dictionary<string, string> { ["os.name"] = "Android" });
    }
}
