using Sentry.Internals;
using Xunit;

namespace Sentry.Tests.Protocol.Contexts
{
    public class ContextsTests
    {
        [Fact]
        public void SerializeObject_NoPropertyFilled_SerializesEmptyObject()
        {
            var sut = new Sentry.Protocol.Contexts();

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{}", actual);
        }

        [Fact]
        public void SerializeObject_SingleDevicePropertySet_SerializeSingleProperty()
        {
            var sut = new Sentry.Protocol.Contexts();
            sut.Device.Architecture = "x86";
            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"device\":{\"arch\":\"x86\"}}", actual);
        }

        [Fact]
        public void SerializeObject_SingleAppPropertySet_SerializeSingleProperty()
        {
            var sut = new Sentry.Protocol.Contexts();
            sut.App.Name = "My.App";
            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"app\":{\"app_name\":\"My.App\"}}", actual);
        }

        [Fact]
        public void SerializeObject_SingleOsPropertySet_SerializeSingleProperty()
        {
            var sut = new Sentry.Protocol.Contexts();
            sut.OperatingSystem.Version = "1.1.1.100";
            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"os\":{\"version\":\"1.1.1.100\"}}", actual);
        }

        [Fact]
        public void SerializeObject_SingleRuntimePropertySet_SerializeSingleProperty()
        {
            var sut = new Sentry.Protocol.Contexts();
            sut.Runtime.Version = "2.1.1.100";
            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"runtime\":{\"version\":\"2.1.1.100\"}}", actual);
        }

        [Fact]
        public void Ctor_SingleBrowserPropertySet_SerializeSingleProperty()
        {
            var contexts = new Sentry.Protocol.Contexts();
            contexts.Browser.Name = "Netscape 1";
            var actual = JsonSerializer.SerializeObject(contexts);

            Assert.Equal("{\"browser\":{\"name\":\"Netscape 1\"}}", actual);
        }

        [Fact]
        public void Ctor_SingleOperatingSystemPropertySet_SerializeSingleProperty()
        {
            var contexts = new Sentry.Protocol.Contexts();
            contexts.OperatingSystem.Name = "BeOS 1";
            var actual = JsonSerializer.SerializeObject(contexts);

            Assert.Equal("{\"os\":{\"name\":\"BeOS 1\"}}", actual);
        }
    }
}
