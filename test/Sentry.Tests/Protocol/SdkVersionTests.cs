using System.Collections.Generic;
using System.Linq;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class SdkVersionTests
    {
        [Fact]
        public void AddPackage_DoesNotExcludeCurrentOne()
        {
            var sut = new SdkVersion();

            sut.AddPackage("Sentry", "1.0");
            sut.AddPackage("Sentry.Log4Net", "10.0");

            Assert.Equal(2, sut.Packages.Count());
        }

        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new SdkVersion
            {
                Name = "Sentry.Test.SDK",
                Version = "0.0.1-preview1",
            };
            sut.AddPackage("Sentry.AspNetCore", "2.0");
            sut.AddPackage("Sentry", "1.0");

            var actual = sut.ToJsonString();

            Assert.Equal(
                "{\"packages\":[{\"name\":\"Sentry\",\"version\":\"1.0\"},{\"name\":\"Sentry.AspNetCore\",\"version\":\"2.0\"}]," +
                "\"name\":\"Sentry.Test.SDK\"," +
                "\"version\":\"0.0.1-preview1\"}",
                actual
            );
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((SdkVersion sdkVersion, string serialized) @case)
        {
            var actual = @case.sdkVersion.ToJsonString();

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new SdkVersion(), "{}") };
            yield return new object[] { (new SdkVersion { Name = "some name" }, "{\"name\":\"some name\"}") };
            yield return new object[] { (new SdkVersion { Version = "some version" }, "{\"version\":\"some version\"}") };
            var sdk = new SdkVersion();
            sdk.AddPackage("b","2");
            sdk.AddPackage("a","1");
            yield return new object[] { (sdk, "{\"packages\":[{\"name\":\"a\",\"version\":\"1\"},{\"name\":\"b\",\"version\":\"2\"}]}") };
        }
    }
}
