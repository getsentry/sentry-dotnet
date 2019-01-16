using System.Collections.Generic;
using Xunit;

namespace Sentry.Protocol.Tests
{
    public class PackageTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new Package("nuget:Sentry", "1.0.0-preview");

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"name\":\"nuget:Sentry\","
                        + "\"version\":\"1.0.0-preview\"}",
                    actual);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((Package msg, string serialized) @case)
        {
            var actual = JsonSerializer.SerializeObject(@case.msg);

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new Package(null, null), "{}") };
            yield return new object[] { (new Package("nuget:Sentry", null), "{\"name\":\"nuget:Sentry\"}") };
            yield return new object[] { (new Package(null, "0.0.0-alpha"), "{\"version\":\"0.0.0-alpha\"}") };
        }
    }
}
