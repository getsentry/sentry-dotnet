using System.Collections.Generic;
using Sentry.Internals;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol.Contexts
{
    public class BrowserTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new Browser
            {
                Version = "6",
                Name = "Internet Explorer",
            };

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"name\":\"Internet Explorer\",\"version\":\"6\"}", actual);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((Browser browser, string serialized) @case)
        {
            var actual = JsonSerializer.SerializeObject(@case.browser);

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new Browser(), "{}") };
            yield return new object[] { (new Browser { Name = "some name" }, "{\"name\":\"some name\"}") };
            yield return new object[] { (new Browser { Version = "some version" }, "{\"version\":\"some version\"}") };
        }
    }
}
