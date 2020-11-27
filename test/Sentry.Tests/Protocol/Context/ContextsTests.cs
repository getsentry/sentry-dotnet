using FluentAssertions;
using Sentry.Internal;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol.Tests.Context
{
    public class ContextsTests
    {
        [Fact]
        public void SerializeObject_NoPropertyFilled_SerializesEmptyObject()
        {
            var sut = new Contexts();

            var actualString = sut.ToJsonString();

            var actual = Contexts.FromJson(Json.Parse(actualString));
            actual.Should().BeEquivalentTo(sut);

            Assert.Equal("{}", actualString);
        }

        [Fact]
        public void SerializeObject_SingleUserDefinedKeyPropertySet_SerializeSingleProperty()
        {
            const string expectedKey = "server";
            var os = new OperatingSystem { Name = "Linux" };
            var sut = new Contexts
            {
                [expectedKey] = os
            };

            var actualString = sut.ToJsonString();

            var actual = Contexts.FromJson(Json.Parse(actualString));
            actual.Should().BeEquivalentTo(sut);

            Assert.Equal("{\"server\":{\"type\":\"os\",\"name\":\"Linux\"}}", actualString);
        }

        [Fact]
        public void SerializeObject_SingleDevicePropertySet_SerializeSingleProperty()
        {
            var sut = new Contexts();
            sut.Device.Architecture = "x86";

            var actualString = sut.ToJsonString();

            var actual = Contexts.FromJson(Json.Parse(actualString));
            actual.Should().BeEquivalentTo(sut);

            Assert.Equal("{\"device\":{\"type\":\"device\",\"arch\":\"x86\"}}", actualString);
        }

        [Fact]
        public void SerializeObject_SingleAppPropertySet_SerializeSingleProperty()
        {
            var sut = new Contexts();
            sut.App.Name = "My.App";

            var actualString = sut.ToJsonString();

            var actual = Contexts.FromJson(Json.Parse(actualString));
            actual.Should().BeEquivalentTo(sut);

            Assert.Equal("{\"app\":{\"type\":\"app\",\"app_name\":\"My.App\"}}", actualString);
        }

        [Fact]
        public void SerializeObject_SingleGpuPropertySet_SerializeSingleProperty()
        {
            var sut = new Contexts();
            sut.Gpu.Name = "My.Gpu";

            var actualString = sut.ToJsonString();

            var actual = Contexts.FromJson(Json.Parse(actualString));
            actual.Should().BeEquivalentTo(sut);

            Assert.Equal("{\"gpu\":{\"type\":\"gpu\",\"name\":\"My.Gpu\"}}", actualString);
        }

        [Fact]
        public void SerializeObject_SingleRuntimePropertySet_SerializeSingleProperty()
        {
            var sut = new Contexts();
            sut.Runtime.Version = "2.1.1.100";

            var actualString = sut.ToJsonString();

            var actual = Contexts.FromJson(Json.Parse(actualString));
            actual.Should().BeEquivalentTo(sut);

            Assert.Equal("{\"runtime\":{\"type\":\"runtime\",\"version\":\"2.1.1.100\"}}", actualString);
        }

        [Fact]
        public void Ctor_SingleBrowserPropertySet_SerializeSingleProperty()
        {
            var sut = new Contexts();
            sut.Browser.Name = "Netscape 1";

            var actualString = sut.ToJsonString();

            var actual = Contexts.FromJson(Json.Parse(actualString));
            actual.Should().BeEquivalentTo(sut);

            Assert.Equal("{\"browser\":{\"type\":\"browser\",\"name\":\"Netscape 1\"}}", actualString);
        }

        [Fact]
        public void Ctor_SingleOperatingSystemPropertySet_SerializeSingleProperty()
        {
            var sut = new Contexts();
            sut.OperatingSystem.Name = "BeOS 1";

            var actualString = sut.ToJsonString();

            var actual = Contexts.FromJson(Json.Parse(actualString));
            actual.Should().BeEquivalentTo(sut);

            Assert.Equal("{\"os\":{\"type\":\"os\",\"name\":\"BeOS 1\"}}", actualString);
        }

        [Fact]
        public void Clone_CopyValues()
        {
            var sut = new Contexts();
            sut.App.Name = "name";
            const string expectedKey = "key";
            var expectedObject = new object();
            sut[expectedKey] = expectedObject;

            var clone = sut.Clone();

            Assert.Equal(sut.App.Name, clone.App.Name);
            Assert.Same(sut[expectedKey], clone[expectedKey]);
        }
    }
}
