namespace Sentry.Tests.Protocol;

public class DeviceTests
{
    [Fact]
    public void Clone_CopyValues()
    {
        // Arrange
        var sut = TestDevice();

        // Act
        var result = sut.Clone();

        // Assert
        AssertAreEqual(result, sut);
    }

    [Fact]
    public void WriteTo_FromJson_Symmetric()
    {
        // Arrange
        var sut = TestDevice();

        var json = sut.ToJsonString();
        using var document = JsonDocument.Parse(json);
        var jsonElement = document.RootElement;

        // Act
        var result = Device.FromJson(jsonElement);

        // Assert
        AssertAreEqual(result, sut);
    }

    [Fact]
    public void FromJson_JavaTypes_CastCorrectly()
    {
        // Arrange
        var sut = TestDevice();

        var json = sut.ToJsonString()
            // In the Java SDK, the Processor Frequency is stored as a Double
            .Replace(@"""processor_frequency"": 12", @"""processor_frequency"": 12.0");

        using var document = JsonDocument.Parse(json);
        var jsonElement = document.RootElement;

        // Act
        var result = Device.FromJson(jsonElement);

        // Assert
        AssertAreEqual(result, sut);
    }

    private static Device TestDevice()
    {
        return new Device
        {
            Name = "TestName",
            Manufacturer = "TestManufacturer",
            Brand = "TestBrand",
            Architecture = "TestArchitecture",
            BatteryLevel = 1,
            IsCharging = true,
            IsOnline = true,
            BootTime = new DateTimeOffset(2001, 06, 15, 12, 30, 0, TimeSpan.Zero),
            ExternalFreeStorage = 2,
            ExternalStorageSize = 3,
            ScreenResolution = "800x600",
            ScreenDensity = 1.2f,
            ScreenDpi = 4,
            Family = "TestFamily",
            FreeMemory = 5,
            FreeStorage = 6,
            MemorySize = 7,
            Model = "TestModel",
            ModelId = "TestModelId",
            Orientation = DeviceOrientation.Landscape,
            Simulator = true,
            StorageSize = 8,
            Timezone = TimeZoneInfo.Utc,
            UsableMemory = 9,
            LowMemory = true,
            ProcessorCount = 11,
            CpuDescription = "TestCpuDescription",
            ProcessorFrequency = 12,
            SupportsVibration = true,
            DeviceType = "TestDeviceType",
            BatteryStatus = "TestBatteryStatus",
            DeviceUniqueIdentifier = "TestDeviceUniqueIdentifier",
            SupportsAccelerometer = true,
            SupportsGyroscope = true,
            SupportsAudio = true,
            SupportsLocationService = true
        };
    }

    private static void AssertAreEqual(Device actual, Device expected)
    {
        using (new AssertionScope())
        {
            actual.Name.Should().Be(expected.Name);
            actual.Manufacturer.Should().Be(expected.Manufacturer);
            actual.Brand.Should().Be(expected.Brand);
            actual.Architecture.Should().Be(expected.Architecture);
            actual.BatteryLevel.Should().Be(expected.BatteryLevel);
            actual.IsCharging.Should().Be(expected.IsCharging);
            actual.IsOnline.Should().Be(expected.IsOnline);
            actual.BootTime.Should().Be(expected.BootTime);
            actual.ExternalFreeStorage.Should().Be(expected.ExternalFreeStorage);
            actual.ExternalStorageSize.Should().Be(expected.ExternalStorageSize);
            actual.ScreenResolution.Should().Be(expected.ScreenResolution);
            actual.ScreenDensity.Should().Be(expected.ScreenDensity);
            actual.ScreenDpi.Should().Be(expected.ScreenDpi);
            actual.Family.Should().Be(expected.Family);
            actual.FreeMemory.Should().Be(expected.FreeMemory);
            actual.FreeStorage.Should().Be(expected.FreeStorage);
            actual.MemorySize.Should().Be(expected.MemorySize);
            actual.Model.Should().Be(expected.Model);
            actual.ModelId.Should().Be(expected.ModelId);
            actual.Orientation.Should().Be(expected.Orientation);
            actual.Simulator.Should().Be(expected.Simulator);
            actual.StorageSize.Should().Be(expected.StorageSize);
            actual.Timezone.Should().Be(expected.Timezone);
            actual.UsableMemory.Should().Be(expected.UsableMemory);
            actual.LowMemory.Should().Be(expected.LowMemory);
            actual.ProcessorCount.Should().Be(expected.ProcessorCount);
            actual.CpuDescription.Should().Be(expected.CpuDescription);
            actual.ProcessorFrequency.Should().Be(expected.ProcessorFrequency);
            actual.SupportsVibration.Should().Be(expected.SupportsVibration);
            actual.DeviceType.Should().Be(expected.DeviceType);
            actual.BatteryStatus.Should().Be(expected.BatteryStatus);
            actual.DeviceUniqueIdentifier.Should().Be(expected.DeviceUniqueIdentifier);
            actual.SupportsAccelerometer.Should().Be(expected.SupportsAccelerometer);
            actual.SupportsGyroscope.Should().Be(expected.SupportsGyroscope);
            actual.SupportsAudio.Should().Be(expected.SupportsAudio);
            actual.SupportsLocationService.Should().Be(expected.SupportsLocationService);
        }
    }
}
