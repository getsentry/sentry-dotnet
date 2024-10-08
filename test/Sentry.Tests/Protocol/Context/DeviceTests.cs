namespace Sentry.Tests.Protocol.Context;

public class DeviceTests
{
    private readonly IDiagnosticLogger _testOutputLogger;
    private const float Delta = 0.0001f;

    public DeviceTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Ctor_NoPropertyFilled_SerializesEmptyObject()
    {
        var sut = new Device();

        var actual = sut.ToJsonString(_testOutputLogger);

        Assert.Equal("""{"type":"device"}""", actual);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var timeZone = TimeZoneInfo.CreateCustomTimeZone(
            "tz_id",
            TimeSpan.FromHours(2),
            "my timezone",
            "my timezone");

        var sut = new Device
        {
            Name = "testing.sentry.io",
            Architecture = "x64",
            BatteryLevel = 99,
            IsCharging = true,
            BootTime = DateTimeOffset.MaxValue,
            ExternalFreeStorage = 100_000_000_000_000, // 100 TB
            ExternalStorageSize = 1_000_000_000_000_000, // 1 PB
            Family = "Windows",
            FreeMemory = 200_000_000_000, // 200 GB
            MemorySize = 500_000_000_000, // 500 GB
            StorageSize = 100_000_000,
            FreeStorage = 0,
            ScreenResolution = "800x600",
            ScreenDensity = 42,
            ScreenDpi = 42,
            Brand = "Brand",
            Manufacturer = "Manufacturer",
            Model = "Windows Server 2012 R2",
            ModelId = "0921309128012",
            Orientation = DeviceOrientation.Portrait,
            Simulator = false,
            Timezone = timeZone,
            UsableMemory = 100,
            LowMemory = true,
            ProcessorCount = 8,
            CpuDescription = "Intel(R) Core(TM)2 Quad CPU Q6600 @ 2.40GHz",
            ProcessorFrequency = 2500,
            DeviceType = "Console",
            BatteryStatus = "Charging",
            DeviceUniqueIdentifier = "d610540d-11d6-4daa-a98c-b71030acae4d",
            SupportsVibration = false,
            SupportsAccelerometer = true,
            SupportsGyroscope = true,
            SupportsAudio = true,
            SupportsLocationService = true
        };

        var actual = sut.ToJsonString(_testOutputLogger, indented: true);

        Assert.Equal("""
            {
              "type": "device",
              "timezone": "tz_id",
              "timezone_display_name": "my timezone",
              "name": "testing.sentry.io",
              "manufacturer": "Manufacturer",
              "brand": "Brand",
              "family": "Windows",
              "model": "Windows Server 2012 R2",
              "model_id": "0921309128012",
              "arch": "x64",
              "battery_level": 99,
              "charging": true,
              "orientation": "portrait",
              "simulator": false,
              "memory_size": 500000000000,
              "free_memory": 200000000000,
              "usable_memory": 100,
              "low_memory": true,
              "storage_size": 100000000,
              "free_storage": 0,
              "external_storage_size": 1000000000000000,
              "external_free_storage": 100000000000000,
              "screen_resolution": "800x600",
              "screen_density": 42,
              "screen_dpi": 42,
              "boot_time": "9999-12-31T23:59:59.9999999+00:00",
              "processor_count": 8,
              "cpu_description": "Intel(R) Core(TM)2 Quad CPU Q6600 @ 2.40GHz",
              "processor_frequency": 2500,
              "device_type": "Console",
              "battery_status": "Charging",
              "device_unique_identifier": "d610540d-11d6-4daa-a98c-b71030acae4d",
              "supports_vibration": false,
              "supports_accelerometer": true,
              "supports_gyroscope": true,
              "supports_audio": true,
              "supports_location_service": true
            }
            """,
            actual);
    }

    [Fact]
    public void Clone_CopyValues()
    {
        var sut = TestDevice();

        var clone = sut.Clone();

        AssertAreEqual(sut, clone);
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
    public void FromJson_NonSystemTimeZone_NoException()
    {
        // Arrange
        const string json = """{"type":"device","timezone":"tz_id","timezone_display_name":"tz_name"}""";

        // Act
        var device = Json.Parse(json, Device.FromJson);

        // Assert
        device.Timezone.Should().NotBeNull();
        device.Timezone?.Id.Should().Be("tz_id");
        device.Timezone?.DisplayName.Should().Be("tz_name");
    }

    [Fact]
    public void FromJson_BatteryLevelFloat()
    {
        // Arrange
        const string json = """{"type":"device","battery_level":1.5}""";

        // Act
        var device = Json.Parse(json, Device.FromJson);

        // Assert
        device.BatteryLevel.Should().NotBeNull();
        device.BatteryLevel?.Should().BeApproximately(1.5f, Delta);
    }

    [Fact]
    public void FromJson_ProcessorFrequencyFloat()
    {
        // Arrange
        const string json = """{"type":"device","processor_frequency":2500.3}""";

        // Act
        var device = Json.Parse(json, Device.FromJson);

        // Assert
        device.ProcessorFrequency.Should().NotBeNull();
        device.ProcessorFrequency?.Should().BeApproximately(2500.3f, Delta);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((Device device, string serialized) @case)
    {
        var actual = @case.device.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new Device(), """{"type":"device"}""") };
        yield return new object[] { (new Device { Name = "some name" }, """{"type":"device","name":"some name"}""") };
        yield return new object[] { (new Device { Orientation = DeviceOrientation.Landscape }, """{"type":"device","orientation":"landscape"}""") };
        yield return new object[] { (new Device { Brand = "some brand" }, """{"type":"device","brand":"some brand"}""") };
        yield return new object[] { (new Device { Manufacturer = "some manufacturer" }, """{"type":"device","manufacturer":"some manufacturer"}""") };
        yield return new object[] { (new Device { Family = "some family" }, """{"type":"device","family":"some family"}""") };
        yield return new object[] { (new Device { Model = "some model" }, """{"type":"device","model":"some model"}""") };
        yield return new object[] { (new Device { ModelId = "some model id" }, """{"type":"device","model_id":"some model id"}""") };
        yield return new object[] { (new Device { Architecture = "some arch" }, """{"type":"device","arch":"some arch"}""") };
        yield return new object[] { (new Device { BatteryLevel = 1 }, """{"type":"device","battery_level":1}""") };
        yield return new object[] { (new Device { BatteryLevel = 1.5f }, """{"type":"device","battery_level":1.5}""") };
        yield return new object[] { (new Device { IsCharging = true }, """{"type":"device","charging":true}""") };
        yield return new object[] { (new Device { IsOnline = true }, """{"type":"device","online":true}""") };
        yield return new object[] { (new Device { Simulator = false }, """{"type":"device","simulator":false}""") };
        yield return new object[] { (new Device { MemorySize = 1 }, """{"type":"device","memory_size":1}""") };
        yield return new object[] { (new Device { FreeMemory = 1 }, """{"type":"device","free_memory":1}""") };
        yield return new object[] { (new Device { UsableMemory = 1 }, """{"type":"device","usable_memory":1}""") };
        yield return new object[] { (new Device { LowMemory = true }, """{"type":"device","low_memory":true}""") };
        yield return new object[] { (new Device { StorageSize = 1 }, """{"type":"device","storage_size":1}""") };
        yield return new object[] { (new Device { FreeStorage = 1 }, """{"type":"device","free_storage":1}""") };
        yield return new object[] { (new Device { ExternalStorageSize = 1 }, """{"type":"device","external_storage_size":1}""") };
        yield return new object[] { (new Device { ExternalFreeStorage = 1 }, """{"type":"device","external_free_storage":1}""") };
        yield return new object[] { (new Device { ScreenResolution = "1x1" }, """{"type":"device","screen_resolution":"1x1"}""") };
        yield return new object[] { (new Device { ScreenDensity = 1 }, """{"type":"device","screen_density":1}""") };
        yield return new object[] { (new Device { ScreenDpi = 1 }, """{"type":"device","screen_dpi":1}""") };
        yield return new object[] { (new Device { BootTime = DateTimeOffset.MaxValue }, """{"type":"device","boot_time":"9999-12-31T23:59:59.9999999+00:00"}""") };
        yield return new object[] { (new Device { Timezone = TimeZoneInfo.CreateCustomTimeZone("tz_id", TimeSpan.Zero, "tz_name", "tz_name") }, """{"type":"device","timezone":"tz_id","timezone_display_name":"tz_name"}""") };
        yield return new object[] { (new Device { Timezone = TimeZoneInfo.CreateCustomTimeZone("tz_id", TimeSpan.Zero, "tz_id", "tz_id") }, """{"type":"device","timezone":"tz_id"}""") };
        yield return new object[] { (new Device { ProcessorCount = 8 }, """{"type":"device","processor_count":8}""") };
        yield return new object[] { (new Device { CpuDescription = "Intel(R) Core(TM)2 Quad CPU Q6600 @ 2.40GHz" }, """{"type":"device","cpu_description":"Intel(R) Core(TM)2 Quad CPU Q6600 @ 2.40GHz"}""") };
        yield return new object[] { (new Device { ProcessorFrequency = 2500 }, """{"type":"device","processor_frequency":2500}""") };
        yield return new object[] { (new Device { ProcessorFrequency = 2500.5f }, """{"type":"device","processor_frequency":2500.5}""") };
        yield return new object[] { (new Device { DeviceType = "Handheld" }, """{"type":"device","device_type":"Handheld"}""") };
        yield return new object[] { (new Device { BatteryStatus = "Charging" }, """{"type":"device","battery_status":"Charging"}""") };
        yield return new object[] { (new Device { DeviceUniqueIdentifier = "d610540d-11d6-4daa-a98c-b71030acae4d" }, """{"type":"device","device_unique_identifier":"d610540d-11d6-4daa-a98c-b71030acae4d"}""") };
        yield return new object[] { (new Device { SupportsVibration = false }, """{"type":"device","supports_vibration":false}""") };
        yield return new object[] { (new Device { SupportsAccelerometer = true }, """{"type":"device","supports_accelerometer":true}""") };
        yield return new object[] { (new Device { SupportsGyroscope = true }, """{"type":"device","supports_gyroscope":true}""") };
        yield return new object[] { (new Device { SupportsAudio = true }, """{"type":"device","supports_audio":true}""") };
        yield return new object[] { (new Device { SupportsLocationService = true }, """{"type":"device","supports_location_service":true}""") };
    }

    private static Device TestDevice()
    {
        return new Device
        {
            Name = "name",
            Brand = "brand",
            Manufacturer = "manufacturer",
            Family = "family",
            Model = "Model",
            ModelId = "ModelId",
            Architecture = "Architecture",
            BatteryLevel = 2,
            IsCharging = false,
            Orientation = DeviceOrientation.Portrait,
            Simulator = true,
            MemorySize = 3,
            FreeMemory = 4,
            UsableMemory = 5,
            LowMemory = false,
            StorageSize = 6,
            FreeStorage = 7,
            ExternalStorageSize = 8,
            ExternalFreeStorage = 9,
            ScreenResolution = "1x1",
            ScreenDensity = 10,
            ScreenDpi = 11,
            BootTime = DateTimeOffset.UtcNow,
            Timezone = TimeZoneInfo.Utc,
            IsOnline = false,
            ProcessorCount = 8,
            CpuDescription = "Intel(R) Core(TM)2 Quad CPU Q6600 @ 2.40GHz",
            ProcessorFrequency = 2500,
            DeviceType = "Console",
            BatteryStatus = "Charging",
            DeviceUniqueIdentifier = "d610540d-11d6-4daa-a98c-b71030acae4d",
            SupportsVibration = false,
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
            actual.BatteryLevel.Should().BeApproximately(expected.BatteryLevel, Delta);
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
            actual.ProcessorFrequency.Should().BeApproximately(expected.ProcessorFrequency, Delta);
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
