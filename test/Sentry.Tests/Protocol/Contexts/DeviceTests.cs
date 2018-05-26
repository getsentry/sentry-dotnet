using System;
using System.Collections.Generic;
using Sentry.Internals;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol.Contexts
{
    public class DeviceTests
    {
        [Fact]
        public void Ctor_NoPropertyFilled_SerializesEmptyObject()
        {
            var sut = new Device();

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{}", actual);
        }

        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new Device
            {
                Name = "testing.sentry.io",
                Architecture = "x64",
                BatteryLevel = 99,
                BootTime = DateTimeOffset.MaxValue,
                ExternalFreeStorage = 100_000_000_000_000, // 100 TB
                ExternalStorageSize = 1_000_000_000_000_000, // 1 PB
                Family = "Windows",
                FreeMemory = 200_000_000_000, // 200 GB
                MemorySize = 500_000_000_000, // 500 GB
                StorageSize = 100_000_000,
                FreeStorage = 0,
                Model = "Windows Server 2012 R2",
                ModelId = "0921309128012",
                Orientation = DeviceOrientation.Portrait,
                Simulator = false,
                Timezone = TimeZoneInfo.Local,
                UsableMemory = 100
            };

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal($"{{\"timezone\":\"{TimeZoneInfo.Local.Id}\"," +
                         "\"name\":\"testing.sentry.io\"," +
                         "\"family\":\"Windows\"," +
                         "\"model\":\"Windows Server 2012 R2\"," +
                         "\"model_id\":\"0921309128012\"," +
                         "\"arch\":\"x64\"," +
                         "\"battery_level\":99," +
                         "\"orientation\":\"portrait\"," +
                         "\"simulator\":false," +
                         "\"memory_size\":500000000000," +
                         "\"free_memory\":200000000000," +
                         "\"usable_memory\":100," +
                         "\"storage_size\":100000000," +
                         "\"free_storage\":0," +
                         "\"external_storage_size\":1000000000000000," +
                         "\"external_free_storage\":100000000000000," +
                         "\"boot_time\":\"9999-12-31T23:59:59.9999999+00:00\"}",
                actual);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((Device device, string serialized) @case)
        {
            var actual = JsonSerializer.SerializeObject(@case.device);

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new Device(), "{}") };
            yield return new object[] { (new Device { Name = "some name" }, "{\"name\":\"some name\"}") };
            yield return new object[] { (new Device { Orientation = DeviceOrientation.Landscape }, "{\"orientation\":\"landscape\"}") };
            yield return new object[] { (new Device { Family = "some family" }, "{\"family\":\"some family\"}") };
            yield return new object[] { (new Device { Model = "some model" }, "{\"model\":\"some model\"}") };
            yield return new object[] { (new Device { ModelId = "some model id" }, "{\"model_id\":\"some model id\"}") };
            yield return new object[] { (new Device { Architecture = "some arch" }, "{\"arch\":\"some arch\"}") };
            yield return new object[] { (new Device { BatteryLevel = 1 }, "{\"battery_level\":1}") };
            yield return new object[] { (new Device { Simulator = false }, "{\"simulator\":false}") };
            yield return new object[] { (new Device { MemorySize = 1 }, "{\"memory_size\":1}") };
            yield return new object[] { (new Device { FreeMemory = 1 }, "{\"free_memory\":1}") };
            yield return new object[] { (new Device { UsableMemory = 1 }, "{\"usable_memory\":1}") };
            yield return new object[] { (new Device { StorageSize = 1 }, "{\"storage_size\":1}") };
            yield return new object[] { (new Device { FreeStorage = 1 }, "{\"free_storage\":1}") };
            yield return new object[] { (new Device { ExternalStorageSize = 1 }, "{\"external_storage_size\":1}") };
            yield return new object[] { (new Device { ExternalFreeStorage = 1 }, "{\"external_free_storage\":1}") };
            yield return new object[] { (new Device { BootTime = DateTimeOffset.MaxValue }, "{\"boot_time\":\"9999-12-31T23:59:59.9999999+00:00\"}") };
            yield return new object[] { (new Device { Timezone = TimeZoneInfo.Utc }, "{\"timezone\":\"UTC\"}") };
        }
    }
}
