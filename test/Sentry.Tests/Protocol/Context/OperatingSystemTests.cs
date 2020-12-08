using System.Collections.Generic;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol.Tests.Context
{
    public class OperatingSystemTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new OperatingSystem
            {
                Name = "Windows",
                KernelVersion = "who knows",
                Version = "2016",
                RawDescription = "Windows 2016",
                Build = "14393",
                Rooted = true
            };

            var actual = sut.ToJsonString();

            Assert.Equal(
                "{\"type\":\"os\"," +
                "\"name\":\"Windows\"," +
                "\"version\":\"2016\"," +
                "\"raw_description\":\"Windows 2016\"," +
                "\"build\":\"14393\"," +
                "\"kernel_version\":\"who knows\"," +
                "\"rooted\":true}",
                actual
            );
        }

        [Fact]
        public void Clone_CopyValues()
        {
            var sut = new OperatingSystem()
            {
                Name = "name",
                KernelVersion = "KernelVersion",
                RawDescription = "RawDescription",
                Rooted = true,
                Build = "build",
                Version = "version"
            };

            var clone = sut.Clone();

            Assert.Equal(sut.Name, clone.Name);
            Assert.Equal(sut.KernelVersion, clone.KernelVersion);
            Assert.Equal(sut.RawDescription, clone.RawDescription);
            Assert.Equal(sut.Rooted, clone.Rooted);
            Assert.Equal(sut.Build, clone.Build);
            Assert.Equal(sut.Version, clone.Version);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((OperatingSystem os, string serialized) @case)
        {
            var actual = @case.os.ToJsonString();

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new OperatingSystem(), "{\"type\":\"os\"}") };
            yield return new object[] { (new OperatingSystem { Name = "some name" }, "{\"type\":\"os\",\"name\":\"some name\"}") };
            yield return new object[] { (new OperatingSystem { RawDescription = "some Name, some version" }, "{\"type\":\"os\",\"raw_description\":\"some Name, some version\"}") };
            yield return new object[] { (new OperatingSystem { Build = "some build" }, "{\"type\":\"os\",\"build\":\"some build\"}") };
            yield return new object[] { (new OperatingSystem { KernelVersion = "some kernel version" }, "{\"type\":\"os\",\"kernel_version\":\"some kernel version\"}") };
            yield return new object[] { (new OperatingSystem { Rooted = false }, "{\"type\":\"os\",\"rooted\":false}") };
        }
    }
}
