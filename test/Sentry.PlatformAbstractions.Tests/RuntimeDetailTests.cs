using System.Collections.Generic;
using System.Security.Cryptography;
using NSubstitute;
using NUnit.Framework;

namespace Sentry.PlatformAbstractions.Tests
{
    public class RuntimeDetailTests
    {
        [Test]
        public void Test1()
        {
            var mock = Substitute.For<HashAlgorithm>();
            mock.DidNotReceive().Initialize();
        }

        [Theory]
        [Test, TestCaseSource(typeof(RuntimeDetailTests), nameof(TestCases))]
        public void Parse_TestCases(TestCase testCase)
        {
            var actual = RuntimeDetail.Parse(testCase.Raw);

            if (testCase.Raw == null)
            {
                Assert.Null(actual);
            }
            else
            {
                Assert.AreEqual(testCase.Name, actual.Name);
                Assert.AreEqual(testCase.Version, actual.Version);
                Assert.AreEqual(testCase.Raw, actual.Raw);
            }
        }

        public static IEnumerable<TestCase> TestCases()
        {
            yield return new TestCase
            {
                Raw = "Mono 5.10.1.47 (2017-12/8eb8f7d5e74 Fri Apr 13 20:18:12 EDT 2018)",
                Name = "Mono",
                Version = "5.10.1.47"
            };
            yield return new TestCase
            {
                Raw = "Mono 5.10.0 (Visual Studio built mono)",
                Name = "Mono",
                Version = "5.10.0"
            };
            yield return new TestCase
            {
                Raw = ".NET Framework 4.0.30319.17020",
                Name = ".NET Framework",
                Version = "4.0.30319.17020"
            };
            yield return new TestCase
            {
                Raw = ".NET Core 1.0",
                Name = ".NET Core",
                Version = "1.0"
            };
            yield return new TestCase
            {
                Raw = ".NET Native 2.1.65535-preview123123",
                Name = ".NET Native",
                Version = "2.1.65535-preview123123"
            };
            yield return new TestCase
            {
                Raw = ".NET Framework For Windows Mobile",
                Name = ".NET Framework For Windows Mobile"
            };
            yield return new TestCase
            {
                Raw = ".NET Framework For Windows Mobile 10",
                Name = ".NET Framework For Windows Mobile",
                Version = "10"
            };
            yield return new TestCase
            {
                Raw = "web",
                Name = "web"
            };
            yield return new TestCase
            {
                Raw = null,
                Name = null,
                Version = null
            };
        }

        public class TestCase
        {
            public string Raw { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
        }
    }
}
