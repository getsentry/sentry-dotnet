using Sentry.Maui.Internal;
using Device = Sentry.Protocol.Device;

namespace Sentry.Maui.Tests.Internal;

public class MauiDeviceDataTests
{
    [Fact]
    public void ApplyMauiDeviceData_Android_ReadsDisplayInfoFromApplicationResources()
    {
        // Arrange
        var device = new Device();
        var metrics = global::Android.App.Application.Context.Resources!.DisplayMetrics!;

        // Act
        device.ApplyMauiDeviceData(null, null);

        // Assert
        device.ScreenResolution.Should().Be($"{metrics.WidthPixels}x{metrics.HeightPixels}");
        device.ScreenDensity.Should().Be(metrics.Density);
        device.ScreenDpi.Should().Be((int)metrics.DensityDpi);
        device.Orientation.Should().NotBeNull();
    }

    [Fact]
    public void ApplyMauiDeviceData_Android_KeepsExistingDisplayInfo()
    {
        // Arrange
        var device = new Device
        {
            ScreenResolution = "1x2",
            ScreenDensity = 3,
            ScreenDpi = 4,
            Orientation = DeviceOrientation.Landscape
        };

        // Act
        device.ApplyMauiDeviceData(null, null);

        // Assert
        device.ScreenResolution.Should().Be("1x2");
        device.ScreenDensity.Should().Be(3);
        device.ScreenDpi.Should().Be(4);
        device.Orientation.Should().Be(DeviceOrientation.Landscape);
    }
}
