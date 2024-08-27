namespace Sentry.Tests.Protocol.Context;

public class DeviceTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

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
        var sut = new Device
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

        var clone = sut.Clone();

        Assert.Equal(sut.Name, clone.Name);
        Assert.Equal(sut.Family, clone.Family);
        Assert.Equal(sut.Brand, clone.Brand);
        Assert.Equal(sut.Manufacturer, clone.Manufacturer);
        Assert.Equal(sut.Model, clone.Model);
        Assert.Equal(sut.ModelId, clone.ModelId);
        Assert.Equal(sut.Architecture, clone.Architecture);
        Assert.Equal(sut.BatteryLevel, clone.BatteryLevel);
        Assert.Equal(sut.IsCharging, clone.IsCharging);
        Assert.Equal(sut.Orientation, clone.Orientation);
        Assert.Equal(sut.Simulator, clone.Simulator);
        Assert.Equal(sut.MemorySize, clone.MemorySize);
        Assert.Equal(sut.FreeMemory, clone.FreeMemory);
        Assert.Equal(sut.LowMemory, clone.LowMemory);
        Assert.Equal(sut.UsableMemory, clone.UsableMemory);
        Assert.Equal(sut.StorageSize, clone.StorageSize);
        Assert.Equal(sut.FreeStorage, clone.FreeStorage);
        Assert.Equal(sut.ExternalStorageSize, clone.ExternalStorageSize);
        Assert.Equal(sut.ExternalFreeStorage, clone.ExternalFreeStorage);
        Assert.Equal(sut.ScreenResolution, clone.ScreenResolution);
        Assert.Equal(sut.ScreenDensity, clone.ScreenDensity);
        Assert.Equal(sut.ScreenDpi, clone.ScreenDpi);
        Assert.Equal(sut.BootTime, clone.BootTime);
        Assert.Equal(sut.Timezone, clone.Timezone);
        Assert.Equal(sut.IsOnline, clone.IsOnline);
        Assert.Equal(sut.ProcessorCount, clone.ProcessorCount);
        Assert.Equal(sut.CpuDescription, clone.CpuDescription);
        Assert.Equal(sut.ProcessorFrequency, clone.ProcessorFrequency);
        Assert.Equal(sut.DeviceType, clone.DeviceType);
        Assert.Equal(sut.BatteryStatus, clone.BatteryStatus);
        Assert.Equal(sut.DeviceUniqueIdentifier, clone.DeviceUniqueIdentifier);
        Assert.Equal(sut.SupportsVibration, clone.SupportsVibration);
        Assert.Equal(sut.SupportsAccelerometer, clone.SupportsAccelerometer);
        Assert.Equal(sut.SupportsGyroscope, clone.SupportsGyroscope);
        Assert.Equal(sut.SupportsAudio, clone.SupportsAudio);
        Assert.Equal(sut.SupportsLocationService, clone.SupportsLocationService);
    }

    [Fact]
    public void TimeZone_Serialisation_Symmetric()
    {
        // Arrange
        var device = new Device {
            Timezone = TimeZoneInfo.CreateCustomTimeZone(
                "tz_id",
                TimeSpan.FromHours(3),
                "display_name",
                "standard_name",
                "daylight_name",
                [TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                    DateTime.MinValue,
                    DateTime.MaxValue,
                    TimeSpan.FromHours(1),
                    TimeZoneInfo.TransitionTime.CreateFixedDateRule(
                        DateTime.MinValue,
                        1,
                        1
                    ),
                    TimeZoneInfo.TransitionTime.CreateFixedDateRule(
                        DateTime.MinValue,
                        2,
                        1
                    )
                )]
            )
        };

        // Act
        var json = device.ToJsonString();
        var result = Json.Parse(json, Device.FromJson);

        // Assert
        result.Timezone.Should().NotBeNull();
        using (new AssertionScope())
        {
            result.Timezone!.Id.Should().Be(device.Timezone.Id);
            result.Timezone!.BaseUtcOffset.Should().Be(device.Timezone.BaseUtcOffset);
            result.Timezone!.DisplayName.Should().Be(device.Timezone.DisplayName);
            result.Timezone!.StandardName.Should().Be(device.Timezone.StandardName);
            result.Timezone!.DaylightName.Should().Be(device.Timezone.DaylightName);
            result.Timezone!.GetAdjustmentRules().Should().BeEquivalentTo(device.Timezone.GetAdjustmentRules());
        }
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((Device device, string serialized) @case)
    {
        var actual = @case.device.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
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
        yield return new object[] { (new Device { DeviceType = "Handheld" }, """{"type":"device","device_type":"Handheld"}""") };
        yield return new object[] { (new Device { BatteryStatus = "Charging" }, """{"type":"device","battery_status":"Charging"}""") };
        yield return new object[] { (new Device { DeviceUniqueIdentifier = "d610540d-11d6-4daa-a98c-b71030acae4d" }, """{"type":"device","device_unique_identifier":"d610540d-11d6-4daa-a98c-b71030acae4d"}""") };
        yield return new object[] { (new Device { SupportsVibration = false }, """{"type":"device","supports_vibration":false}""") };
        yield return new object[] { (new Device { SupportsAccelerometer = true }, """{"type":"device","supports_accelerometer":true}""") };
        yield return new object[] { (new Device { SupportsGyroscope = true }, """{"type":"device","supports_gyroscope":true}""") };
        yield return new object[] { (new Device { SupportsAudio = true }, """{"type":"device","supports_audio":true}""") };
        yield return new object[] { (new Device { SupportsLocationService = true }, """{"type":"device","supports_location_service":true}""") };
    }
}
