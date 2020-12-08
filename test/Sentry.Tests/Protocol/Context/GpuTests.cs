using System.Collections.Generic;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol.Tests.Context
{
    public class GpuTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new Gpu
            {
                Name = "Sentry.Test.Gpu",
                Id = 123,
                VendorId = "321",
                VendorName = "Vendor name",
                MemorySize = 1_000,
                ApiType = "API Type",
                MultiThreadedRendering = true,
                Version = "Version 3232",
                NpotSupport = "Full NPOT",
            };

            var actual = sut.ToJsonString();

            Assert.Equal(
                "{\"type\":\"gpu\"," +
                "\"name\":\"Sentry.Test.Gpu\"," +
                "\"id\":123," +
                "\"vendor_id\":\"321\"," +
                "\"vendor_name\":\"Vendor name\"," +
                "\"memory_size\":1000," +
                "\"api_type\":\"API Type\"," +
                "\"multi_threaded_rendering\":true," +
                "\"version\":\"Version 3232\"," +
                "\"npot_support\":\"Full NPOT\"}",
                actual
            );
        }

        [Fact]
        public void Clone_CopyValues()
        {
            var sut = new Gpu()
            {
                Name = "Sentry.Test.Gpu",
                Id = 123,
                VendorId = "321",
                VendorName = "Vendor name",
                MemorySize = 1_000,
                ApiType = "API Type",
                MultiThreadedRendering = true,
                Version = "Version 3232",
                NpotSupport = "Full NPOT",
            };

            var clone = sut.Clone();

            Assert.Equal(sut.Name, clone.Name);
            Assert.Equal(sut.Id, clone.Id);
            Assert.Equal(sut.VendorId, clone.VendorId);
            Assert.Equal(sut.VendorName, clone.VendorName);
            Assert.Equal(sut.ApiType, clone.ApiType);
            Assert.Equal(sut.MultiThreadedRendering, clone.MultiThreadedRendering);
            Assert.Equal(sut.Version, clone.Version);
            Assert.Equal(sut.NpotSupport, clone.NpotSupport);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((Gpu gpu, string serialized) @case)
        {
            var actual = @case.gpu.ToJsonString();

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new Gpu(), "{\"type\":\"gpu\"}") };
            yield return new object[] { (new Gpu { Name = "some name" }, "{\"type\":\"gpu\",\"name\":\"some name\"}") };
            yield return new object[] { (new Gpu { Id = 1 }, "{\"type\":\"gpu\",\"id\":1}") };
            yield return new object[] { (new Gpu { VendorId = "1" }, "{\"type\":\"gpu\",\"vendor_id\":\"1\"}") };
            yield return new object[] { (new Gpu { VendorName = "some name" }, "{\"type\":\"gpu\",\"vendor_name\":\"some name\"}") };
            yield return new object[] { (new Gpu { MemorySize = 123 }, "{\"type\":\"gpu\",\"memory_size\":123}") };
            yield return new object[] { (new Gpu { ApiType = "some ApiType" }, "{\"type\":\"gpu\",\"api_type\":\"some ApiType\"}") };
            yield return new object[] { (new Gpu { MultiThreadedRendering = false }, "{\"type\":\"gpu\",\"multi_threaded_rendering\":false}") };
            yield return new object[] { (new Gpu { Version = "some version" }, "{\"type\":\"gpu\",\"version\":\"some version\"}") };
            yield return new object[] { (new Gpu { NpotSupport = "some npot_support" }, "{\"type\":\"gpu\",\"npot_support\":\"some npot_support\"}") };
        }
    }
}
