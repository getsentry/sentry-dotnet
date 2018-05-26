using System.Collections.Generic;
using Sentry.Internals;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol.Contexts
{
    public class RuntimeTests 
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new Runtime
            {
                Version = "4.7.2",
                Name = ".NET Framework",
                Build = "461814",
                RawDescription = ".NET Framework 4.7.2"
            };

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"name\":\".NET Framework\",\"version\":\"4.7.2\",\"raw_description\":\".NET Framework 4.7.2\",\"build\":\"461814\"}", actual);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((Runtime runtime, string serialized) @case)
        {
            var actual = JsonSerializer.SerializeObject(@case.runtime);

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new Runtime(), "{}") };
            yield return new object[] { (new Runtime { Name = "some name" }, "{\"name\":\"some name\"}") };
            yield return new object[] { (new Runtime { Version = "some version" }, "{\"version\":\"some version\"}") };
            yield return new object[] { (new Runtime { Build = "some build" }, "{\"build\":\"some build\"}") };
            yield return new object[] { (new Runtime { RawDescription = "some Name, some version" }, "{\"raw_description\":\"some Name, some version\"}") };
        }
    }
}
