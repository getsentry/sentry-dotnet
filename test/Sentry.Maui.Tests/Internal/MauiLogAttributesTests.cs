using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests.Internal;

public class MauiLogAttributesTests
{
    private static IDeviceInfo CreateDeviceInfo(DevicePlatform platform, string model = "Pixel 9", string manufacturer = "Google")
    {
        var deviceInfo = Substitute.For<IDeviceInfo>();
        deviceInfo.Platform.Returns(platform);
        deviceInfo.VersionString.Returns("15");
        deviceInfo.Model.Returns(model);
        deviceInfo.Manufacturer.Returns(manufacturer);
        return deviceInfo;
    }

    [Fact]
    public void Create_KnownPlatform_ReturnsDeviceAndOsAttributes()
    {
        var attributes = MauiLogAttributes.Create(CreateDeviceInfo(DevicePlatform.Android), null);

#if ANDROID
        var expectedBrand = global::Android.OS.Build.Brand;
#else
        var expectedBrand = "Google";
#endif
        attributes.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["os.name"] = "Android",
            ["os.version"] = "15",
            ["device.brand"] = expectedBrand!,
            ["device.model"] = "Pixel 9",
        });
    }

    [Fact]
    public void Create_UnknownPlatform_ReturnsNone()
    {
        var attributes = MauiLogAttributes.Create(CreateDeviceInfo(DevicePlatform.Unknown), null);

        attributes.Should().BeEmpty();
    }

    [Fact]
    public void Create_BlankValue_OmitsAttribute()
    {
        var attributes = MauiLogAttributes.Create(CreateDeviceInfo(DevicePlatform.Android, model: " "), null);

        attributes.Should().NotContain(a => a.Key == "device.model");
        attributes.Should().Contain(a => a.Key == "os.name");
    }

    [Fact]
    public void Create_DeviceInfoThrows_LogsAndReturnsNone()
    {
        var deviceInfo = Substitute.For<IDeviceInfo>();
        deviceInfo.Platform.Throws(new InvalidOperationException());
        var logger = new InMemoryDiagnosticLogger();

        var attributes = MauiLogAttributes.Create(deviceInfo, logger);

        attributes.Should().BeEmpty();
        logger.Entries.Should().Contain(e => e.Level == SentryLevel.Error);
    }
}
