using System;
using System.Collections.Generic;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol.Tests.Context
{
    public class DeviceTests
    {
        [Fact]
        public void Ctor_NoPropertyFilled_SerializesEmptyObject()
        {
            var sut = new Device();

            var actual = sut.ToJsonString();

            Assert.Equal("{\"type\":\"device\"}", actual);
        }

        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
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
                Timezone = TimeZoneInfo.Local,
                UsableMemory = 100,
                LowMemory = true
            };

            var actual = sut.ToJsonString();

            Assert.Equal(
                "{\"type\":\"device\"," +
                $"\"timezone\":\"{TimeZoneInfo.Local.Id}\"," +
                $"\"timezone_display_name\":\"{TimeZoneInfo.Local.DisplayName.Replace("+", "\\u002B")}\"," +
                "\"name\":\"testing.sentry.io\"," +
                "\"manufacturer\":\"Manufacturer\"," +
                "\"brand\":\"Brand\"," +
                "\"family\":\"Windows\"," +
                "\"model\":\"Windows Server 2012 R2\"," +
                "\"model_id\":\"0921309128012\"," +
                "\"arch\":\"x64\"," +
                "\"battery_level\":99," +
                "\"charging\":true," +
                "\"orientation\":\"portrait\"," +
                "\"simulator\":false," +
                "\"memory_size\":500000000000," +
                "\"free_memory\":200000000000," +
                "\"usable_memory\":100," +
                "\"low_memory\":true," +
                "\"storage_size\":100000000," +
                "\"free_storage\":0," +
                "\"external_storage_size\":1000000000000000," +
                "\"external_free_storage\":100000000000000," +
                "\"screen_resolution\":\"800x600\"," +
                "\"screen_density\":42," +
                "\"screen_dpi\":42," +
                "\"boot_time\":\"9999-12-31T23:59:59.9999999+00:00\"}",
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
                IsOnline = false
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
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((Device device, string serialized) @case)
        {
            var actual = @case.device.ToJsonString();

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new Device(), "{\"type\":\"device\"}") };
            yield return new object[] { (new Device { Name = "some name" }, "{\"type\":\"device\",\"name\":\"some name\"}") };
            yield return new object[] { (new Device { Orientation = DeviceOrientation.Landscape }, "{\"type\":\"device\",\"orientation\":\"landscape\"}") };
            yield return new object[] { (new Device { Brand = "some brand" }, "{\"type\":\"device\",\"brand\":\"some brand\"}") };
            yield return new object[] { (new Device { Manufacturer = "some manufacturer" }, "{\"type\":\"device\",\"manufacturer\":\"some manufacturer\"}") };
            yield return new object[] { (new Device { Family = "some family" }, "{\"type\":\"device\",\"family\":\"some family\"}") };
            yield return new object[] { (new Device { Model = "some model" }, "{\"type\":\"device\",\"model\":\"some model\"}") };
            yield return new object[] { (new Device { ModelId = "some model id" }, "{\"type\":\"device\",\"model_id\":\"some model id\"}") };
            yield return new object[] { (new Device { Architecture = "some arch" }, "{\"type\":\"device\",\"arch\":\"some arch\"}") };
            yield return new object[] { (new Device { BatteryLevel = 1 }, "{\"type\":\"device\",\"battery_level\":1}") };
            yield return new object[] { (new Device { IsCharging = true }, "{\"type\":\"device\",\"charging\":true}") };
            yield return new object[] { (new Device { IsOnline = true }, "{\"type\":\"device\",\"online\":true}") };
            yield return new object[] { (new Device { Simulator = false }, "{\"type\":\"device\",\"simulator\":false}") };
            yield return new object[] { (new Device { MemorySize = 1 }, "{\"type\":\"device\",\"memory_size\":1}") };
            yield return new object[] { (new Device { FreeMemory = 1 }, "{\"type\":\"device\",\"free_memory\":1}") };
            yield return new object[] { (new Device { UsableMemory = 1 }, "{\"type\":\"device\",\"usable_memory\":1}") };
            yield return new object[] { (new Device { LowMemory = true }, "{\"type\":\"device\",\"low_memory\":true}") };
            yield return new object[] { (new Device { StorageSize = 1 }, "{\"type\":\"device\",\"storage_size\":1}") };
            yield return new object[] { (new Device { FreeStorage = 1 }, "{\"type\":\"device\",\"free_storage\":1}") };
            yield return new object[] { (new Device { ExternalStorageSize = 1 }, "{\"type\":\"device\",\"external_storage_size\":1}") };
            yield return new object[] { (new Device { ExternalFreeStorage = 1 }, "{\"type\":\"device\",\"external_free_storage\":1}") };
            yield return new object[] { (new Device { ScreenResolution = "1x1" }, "{\"type\":\"device\",\"screen_resolution\":\"1x1\"}") };
            yield return new object[] { (new Device { ScreenDensity = 1 }, "{\"type\":\"device\",\"screen_density\":1}") };
            yield return new object[] { (new Device { ScreenDpi = 1 }, "{\"type\":\"device\",\"screen_dpi\":1}") };
            yield return new object[] { (new Device { BootTime = DateTimeOffset.MaxValue }, "{\"type\":\"device\",\"boot_time\":\"9999-12-31T23:59:59.9999999+00:00\"}") };
            yield return new object[] { (new Device { Timezone = TimeZoneInfo.Utc }, $"{{\"type\":\"device\",\"timezone\":\"UTC\",\"timezone_display_name\":\"{TimeZoneInfo.Utc.DisplayName}\"}}") };
        }
    }
}
