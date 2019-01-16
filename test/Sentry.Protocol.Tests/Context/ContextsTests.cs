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

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{}", actual);
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

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"server\":{\"type\":\"os\",\"name\":\"Linux\"}}", actual);
        }

        [Fact]
        public void SerializeObject_SingleDevicePropertySet_SerializeSingleProperty()
        {
            var sut = new Contexts();
            sut.Device.Architecture = "x86";
            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"device\":{\"type\":\"device\",\"arch\":\"x86\"}}", actual);
        }

        [Fact]
        public void SerializeObject_SingleAppPropertySet_SerializeSingleProperty()
        {
            var sut = new Contexts();
            sut.App.Name = "My.App";
            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"app\":{\"type\":\"app\",\"app_name\":\"My.App\"}}", actual);
        }

        [Fact]
        public void SerializeObject_SingleGpuPropertySet_SerializeSingleProperty()
        {
            var sut = new Contexts();
            sut.Gpu.Name = "My.Gpu";
            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"gpu\":{\"type\":\"gpu\",\"name\":\"My.Gpu\"}}", actual);
        }

        [Fact]
        public void SerializeObject_SingleRuntimePropertySet_SerializeSingleProperty()
        {
            var sut = new Contexts();
            sut.Runtime.Version = "2.1.1.100";
            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"runtime\":{\"type\":\"runtime\",\"version\":\"2.1.1.100\"}}", actual);
        }

        [Fact]
        public void Ctor_SingleBrowserPropertySet_SerializeSingleProperty()
        {
            var contexts = new Contexts();
            contexts.Browser.Name = "Netscape 1";
            var actual = JsonSerializer.SerializeObject(contexts);

            Assert.Equal("{\"browser\":{\"type\":\"browser\",\"name\":\"Netscape 1\"}}", actual);
        }

        [Fact]
        public void Ctor_SingleOperatingSystemPropertySet_SerializeSingleProperty()
        {
            var contexts = new Contexts();
            contexts.OperatingSystem.Name = "BeOS 1";
            var actual = JsonSerializer.SerializeObject(contexts);

            Assert.Equal("{\"os\":{\"type\":\"os\",\"name\":\"BeOS 1\"}}", actual);
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
