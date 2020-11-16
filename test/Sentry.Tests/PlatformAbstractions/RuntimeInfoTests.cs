using System.Collections.Generic;
using Sentry.PlatformAbstractions;
using Xunit;

namespace Sentry.Tests.PlatformAbstractions
{
    public class RuntimeInfoTests
    {
        [Fact] // Verifies that some value is extracted anywhere the tests runs
        public void GetRuntime_NotNull()
        {
            var actual = RuntimeInfo.GetRuntime();
            Assert.NotNull(actual);
            Assert.NotNull(actual.Name);
            Assert.NotNull(actual.Version);
        }

        [Theory]
        [MemberData(nameof(ParseTestCases))]
        public void Parse_TestCases(ParseTestCase parseTestCase)
        {
            var actual = RuntimeInfo.Parse(parseTestCase.Raw,
                    parseTestCase.NameProvided);

            if (parseTestCase.Raw == null)
            {
                if (parseTestCase.NameProvided == null)
                {
                    Assert.Null(actual);
                }
                else
                {
                    Assert.Equal(parseTestCase.NameProvided, actual.Name);
                }
            }
            else
            {
                Assert.Equal(parseTestCase.ExpectedName, actual.Name);
                Assert.Equal(parseTestCase.ExpectedVersion, actual.Version);
                Assert.Equal(parseTestCase.Raw, actual.Raw);
            }
        }

#if NETFX
        [SkippableFact]
        public void SetReleaseAndVersionNetFx_OnNetFx_NonNullReleaseAndVersion()
        {
            // This test is only relevant when running on CLR.
            Skip.If(RuntimeInfo.GetRuntime().IsMono());

            var input = new Runtime(".NET Framework");
            RuntimeInfo.SetNetFxReleaseAndVersion(input);

            Assert.NotNull(input.Version);
            Assert.NotNull(input.FrameworkInstallation);
            Assert.NotNull(input.FrameworkInstallation.Version);
        }
#endif

#if NETCOREAPP
        [Fact]
        public void SetNetCoreVersion_NetCoreAsName()
        {
            var input = new Runtime(".NET Core");
            RuntimeInfo.SetNetCoreVersion(input);

            Assert.NotNull(input.Version);
            Assert.Equal(".NET Core", input.Name);
        }
#endif

#if NET5_0
        [Fact]
        public void SetNetCoreVersion_Net5Runtime_NullNetCoreVersion()
        {
            var input = new Runtime(".NET");
            RuntimeInfo.SetNetCoreVersion(input);

            Assert.Equal(".NET", input.Name);
            Assert.Null(input.Version);
        }
#endif

        public static IEnumerable<object[]> ParseTestCases()
        {
            yield return new object[] { new ParseTestCase
            {
                Raw = "Mono 5.10.1.47 (2017-12/8eb8f7d5e74 Fri Apr 13 20:18:12 EDT 2018)",
                ExpectedName = "Mono",
                ExpectedVersion = "5.10.1.47"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = "5.10.1.47 (2017-12/8eb8f7d5e74 Fri Apr 13 20:18:12 EDT 2018)",
                NameProvided = "Mono",
                ExpectedName = "Mono",
                ExpectedVersion = "5.10.1.47"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = "Mono 5.10.0 (Visual Studio built mono)",
                ExpectedName = "Mono",
                ExpectedVersion = "5.10.0"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = ".NET Framework 4.0.30319.17020",
                ExpectedName = ".NET Framework",
                ExpectedVersion = "4.0.30319.17020"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = "4.0.30319.17020",
                NameProvided = ".NET Framework",
                ExpectedName = ".NET Framework",
                ExpectedVersion = "4.0.30319.17020"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = ".NET Core 1.0",
                ExpectedName = ".NET Core",
                ExpectedVersion = "1.0"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = "1.0",
                NameProvided = ".NET Core",
                ExpectedName = ".NET Core",
                ExpectedVersion = "1.0"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = ".NET Native 2.1.65535-preview123123",
                ExpectedName = ".NET Native",
                ExpectedVersion = "2.1.65535-preview123123"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = ".NET Framework For Windows Mobile 10"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = ".NET Framework For Windows Mobile 10.1",
                ExpectedName = ".NET Framework For Windows Mobile",
                ExpectedVersion = "10.1"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = "web"
            }};
            yield return new object[] { new ParseTestCase
            {
                NameProvided = "web",
                ExpectedName = "web"
            }};
            yield return new object[] { new ParseTestCase
            {
                Raw = null,
                ExpectedName = null,
                ExpectedVersion = null
            }};
        }

        public class ParseTestCase
        {
            public string Raw { get; set; }
            public string NameProvided { get; set; }
            public string ExpectedName { get; set; }
            public string ExpectedVersion { get; set; }
        }
    }
}
